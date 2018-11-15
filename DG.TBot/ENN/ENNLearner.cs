using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.ENN
{
    public class ENNLearner<T, V>
    {
        private List<ENNLayer> currentNetwork;

        private int inputSize;
        private int outputSize;
        private IInterpreter<T> inputInterpreter;
        private IInterpreter<V> outputInterpreter;

        /// <summary>
        /// The current Delta of this algorithm, how many bits the algorithm correctly chooses
        /// </summary>
        public int? Delta { get; private set; }

        public double Loss { get; private set; }

        /// <summary>
        /// How many distinct deltas the last generation had
        /// </summary>
        public int DeltaD { get; private set; }

        public int Momentum { get; private set; }

        public double LearningRate { get; private set; }
        public int Generation { get; private set; }

        private List<TrainingData<T, V>> trainingData;
        private double loss_p;

        public ENNLearner(IInterpreter<T> inputInterpreter, IInterpreter<V> outputInterpreter, int inputSize, int outputSize)
        {
            this.inputSize = inputSize * 32;
            this.outputSize = outputSize * 32;
            this.inputInterpreter = inputInterpreter;
            this.outputInterpreter = outputInterpreter;

            currentNetwork = new List<ENNLayer>();

            currentNetwork.Add(new ENNLayer(this.inputSize, 5));
            currentNetwork.Add(new ENNLayer(5, 50));
            currentNetwork.Add(new ENNLayer(50, 5));
            currentNetwork.Add(new ENNLayer(5, this.outputSize));
            LearningRate = 0.1;
            SetLearningRate(LearningRate);
            trainingData = new List<TrainingData<T, V>>();
            Generation = 0;
        }

        public void AddTrainingData(params TrainingData<T, V>[] data)
        {
            trainingData.AddRange(data);
        }

        public void Evolve(int generations, int offspring)
        {
            loss_p = Math.Log(trainingData.Count);
            for (int generation = 0; generation < generations; generation++)
            {
                List<ENNLayer>[] networks = new List<ENNLayer>[offspring + 1];
                networks[0] = currentNetwork;
                Dictionary<List<ENNLayer>, int> correct = new Dictionary<List<ENNLayer>, int>();
                Dictionary<List<ENNLayer>, int> deltas = new Dictionary<List<ENNLayer>, int>();
                Dictionary<List<ENNLayer>, long> results = new Dictionary<List<ENNLayer>, long>();
                correct[currentNetwork] = 0;
                deltas[currentNetwork] = 0;
                results[currentNetwork] = 0;
                for (int offspringIndex = 0; offspringIndex < offspring; offspringIndex++)
                {
                    networks[offspringIndex + 1] = currentNetwork.CreateOffspring();
                    correct[networks[offspringIndex + 1]] = 0;
                    deltas[networks[offspringIndex + 1]] = 0;
                    results[networks[offspringIndex + 1]] = 0;
                }

                int[] actualBitArray = new int[0];
                foreach (var data in trainingData)
                {
                    foreach (var network in networks)
                    {
                        var input = data.Input.Select(i => inputInterpreter.Encode(i)).SelectMany(i => i.ToBitArray()).ToArray();

                        var output = ForwardLayers(network, input, out actualBitArray);
                        var expectedBitArray = data.ExpectedOutput.SelectMany(o => outputInterpreter.Encode(o).ToBitArray()).ToArray();

                        bool completeCorrect = true;
                        for (int bit = 0; bit < actualBitArray.Length; bit++)
                        {
                            if (actualBitArray[bit] != expectedBitArray[bit])
                            {
                                deltas[network]++;
                                completeCorrect = false;
                            }
                        }
                        if (completeCorrect)
                        {
                            correct[network]++;
                        }

                        for (int i = 0; i < output.Length; i++)
                        {
                            int expectedOutput = outputInterpreter.Encode(data.ExpectedOutput[i]);
                            int actualOutput = output[i];

                            int delta = (expectedOutput - actualOutput) == int.MinValue ? int.MaxValue : Math.Abs(expectedOutput - actualOutput);
                            results[network] += delta;
                            if (delta == 0)
                            {
                                correct[network]++;
                            }
                        }
                    }
                }
                
                var diffs = deltas.ToList().Select(kv => kv.Value).Distinct().OrderBy(d => d).ToList();
                var corrects = correct.ToList().Count(kv => kv.Value > 0);
                if (diffs[0] == 0)
                {

                }
                currentNetwork = networks.OrderBy(n => deltas[n]).ThenByDescending(n => correct[n]).ThenBy(n => results[n]).First();

                if (Delta != deltas[currentNetwork])
                {
                    Momentum = Delta == null ? 0 : 10;
                }
                else
                {
                    Momentum--;
                }

                Delta = deltas[currentNetwork];
                DeltaD = diffs.Count;
                Loss = (double)Delta.Value / (actualBitArray.Length * trainingData.Count);

                if (loss_p - Loss > 0)
                {
                    LearningRate *= 1.1;
                }
                else
                {
                    LearningRate *= 0.95;
                }
                loss_p = loss_p * 0.8 + Loss * 0.2;

                LearningRate = LearningRate > 25 ? 0.1 : LearningRate;
                SetLearningRate(LearningRate);

                Generation++;
            }
        }

        public V[] Ask(params T[] input)
        {
            var encodedInput = input.Select(i => inputInterpreter.Encode(i)).SelectMany(i => i.ToBitArray()).ToArray();
            int[] bitArray;
            var output = ForwardLayers(currentNetwork, encodedInput, out bitArray);
            return output.Select(o => outputInterpreter.Decode(o)).ToArray();
        }

        private int[] ForwardLayers(List<ENNLayer> network, int[] input, out int[] unDecodedBitArray)
        {
            double[] output = null;
            foreach (var layer in network)
            {
                if (output == null)
                {
                    output = layer.Forward(input.Select(Convert.ToDouble).ToArray());
                    continue;
                }
                output = layer.Forward(output);
            }

            int[] decodedOutput = new int[output.Length / 32];
            unDecodedBitArray = output.Select(o => o > 0.5 ? 1 : 0).ToArray();

            for (int skip = 0, i = 0; skip < unDecodedBitArray.Length; skip+=32, i++)
            {
                decodedOutput[i] = unDecodedBitArray.Skip(skip).Take(32).ToArray().ToInt();
            }

            return decodedOutput;
        }

        private void SetLearningRate(double learningRate)
        {
            foreach (var layer in currentNetwork)
            {
                layer.LearningRate = learningRate;
            }
        }
        public event LearningHandler Learning;
    }
}
