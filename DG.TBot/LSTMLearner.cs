using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.TBot
{
    public class LSTMLearner<T>
    {
        private const int BUFFERSIZE = 24;

        private IInterpreter<T> interpreter;

        private Random rnd = new Random();
        private List<T> input;
        private double loss;
        private double loss_p;
        private int size_vocab;
        private int iteration = 0;
        private double learningRate;

        public int Iteration { get { return iteration; } }
        public double LearningRate { get { return learningRate; } }
        public double EstimatedLoss { get { return loss; } }
        public int Parameters { get; private set; }

        // Network layers.
        private List<Layer> layers;

        public LSTMLearner(IEnumerable<T> input, IInterpreter<T> interpreter, int hiddenLayer1 = 264, params int[] hiddenLayers)
        {
            this.interpreter = interpreter;
            this.input = interpreter.CleanUp(input);
            size_vocab = interpreter.VocabularySize;
            loss_p = Math.Log(size_vocab);

            layers = new List<Layer>();
            learningRate = 1e-3;
            SetLearningRate(learningRate);
            layers.Add(new LSTMLayer(size_vocab, hiddenLayer1, BUFFERSIZE)); //input layer
            int nextSize = hiddenLayers.Length == 0 ? hiddenLayer1 : hiddenLayers[0];
            layers.Add(new LSTMLayer(hiddenLayer1, nextSize, BUFFERSIZE));
            for (int i = 0; i < hiddenLayers.Length; i++)
            {
                nextSize = i + 1 >= hiddenLayers.Length ? hiddenLayers[i] : hiddenLayers[i + 1];
                layers.Add(new LSTMLayer(hiddenLayers[i], nextSize, BUFFERSIZE));
            }
            int previousSize = hiddenLayers.Length == 0 ? hiddenLayer1 : hiddenLayers[hiddenLayers.Length - 1];
            layers.Add(new SoftMaxLayer(previousSize, size_vocab, BUFFERSIZE)); //output layer

            Parameters = layers.Sum(l => l.Count());
        }

        public void Learn(int iterations)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                var pos = 0;
                while (pos + BUFFERSIZE < input.Count)
                {
                    // Fill buffer.
                    var buffer = FillBuffer(pos);

                    // Forward propagate activation.
                    var reset = pos == 0;
                    double[][] probs = null;
                    foreach (var layer in layers)
                    {
                        if (probs == null)
                        {
                            probs = layer.Forward(buffer, reset);
                            continue;
                        }
                        probs = layer.Forward(probs, reset);
                    }

                    // Advance buffer.                       
                    var vx = new double[size_vocab];
                    pos += BUFFERSIZE - 1;
                    vx[interpreter.Encode(input[pos])] = 1;
                    AdvanceBuffer(buffer, vx);

                    // Calculate loss.
                    var grads = Loss(probs, buffer);

                    // Backward propagate gradients.
                    foreach (var layer in layers.Reverse<Layer>())
                    {
                        grads = layer.Backward(grads);
                    }
                    double progress = (double)pos / (input.Count - BUFFERSIZE);
                    Learning?.Invoke(this, new LearningEvent(progress, iteration, learningRate));
                }

                // Adjust learning rate.
                if (loss_p - loss > 0)
                {
                    learningRate *= 1.01;
                }
                else
                {
                    learningRate *= 0.98;
                }
                loss_p = loss_p * 0.8 + loss * 0.2;
                SetLearningRate(learningRate);
                iteration++;
            }
        }

        /// <summary>
        /// Calculate cross entropy loss.
        /// </summary>
        private double[][] Loss(double[][] probs, double[][] targets)
        {
            var ls = 0.0;
            var grads = new double[BUFFERSIZE][];
            for (var t = 1; t < BUFFERSIZE; t++)
            {
                grads[t] = probs[t].ToArray();
                for (var i = 0; i < size_vocab; i++)
                {
                    ls += -Math.Log(probs[t][i]) * targets[t][i];
                    grads[t][i] -= targets[t][i];
                }
            }
            ls = ls / (BUFFERSIZE - 1);
            loss = loss * 0.99 + ls * 0.01;
            return grads;
        }

        /// <summary>
        /// Fill buffer with specified number of characters.
        /// </summary>
        private double[][] FillBuffer(int offset)
        {
            // First position is unused.
            var buffer = new double[BUFFERSIZE][];
            for (var pos = 1; pos < BUFFERSIZE; pos++)
            {
                buffer[pos] = new double[size_vocab];
                buffer[pos][interpreter.Encode(input[pos + offset - 1])] = 1;
            }
            return buffer;
        }

        /// <summary>
        /// Read next character into buffer.
        /// </summary>
        private static void AdvanceBuffer(double[][] buffer, double[] vx)
        {
            for (var b = 1; b < BUFFERSIZE - 1; b++)
            {
                buffer[b] = buffer[b + 1];
            }
            buffer[BUFFERSIZE - 1] = vx;
        }

        /// <summary>
        /// Generate sequence of text using trained network.
        /// </summary>
        public List<T> Generate(int length)
        {
            List<T> output = new List<T>();
            var buffer = FillBuffer(0);
            for (var pos = 0; pos < length; pos++)
            {
                var reset = pos == 0;
                double[][] probs = null;
                foreach (var layer in layers)
                {
                    if (probs == null)
                    {
                        probs = layer.Forward(buffer, reset);
                        continue;
                    }
                    probs = layer.Forward(probs, reset);
                }
                var ix = WeightedChoice(probs[BUFFERSIZE - 1]);
                var vx = new double[size_vocab];
                vx[ix] = 1;
                AdvanceBuffer(buffer, vx);
                output.Add(interpreter.Decode(ix));
                Generating?.Invoke(this, new GeneratingEvent((double)output.Count / length));
            }
            return output;
        }

        /// <summary>
        /// Generate sequence of text using trained network.
        /// </summary>
        public List<T> Generate(int length, IEnumerable<T> prefix)
        {
            List<T> output = new List<T>();
            var buffer = FillBuffer(0);
            foreach (var entity in prefix)
            {
                var ix = interpreter.Encode(entity);
                var vx = new double[size_vocab];
                vx[ix] = 1;
                AdvanceBuffer(buffer, vx);
                output.Add(interpreter.Decode(ix));
            }
            for (var pos = 0; pos < length; pos++)
            {
                var reset = pos == 0;
                double[][] probs = null;
                foreach (var layer in layers)
                {
                    if (probs == null)
                    {
                        probs = layer.Forward(buffer, reset);
                        continue;
                    }
                    probs = layer.Forward(probs, reset);
                }
                var ix = WeightedChoice(probs[BUFFERSIZE - 1]);
                var vx = new double[size_vocab];
                vx[ix] = 1;
                AdvanceBuffer(buffer, vx);
                output.Add(interpreter.Decode(ix));
                Generating?.Invoke(this, new GeneratingEvent((double)output.Count / length));
            }
            return output;
        }

        /// <summary>
        ///  Select next character from weighted random distribution.
        /// </summary>
        private int WeightedChoice(double[] vy)
        {
            var val = rnd.NextDouble();
            for (var i = 0; i < vy.Length; i++)
            {
                if (val <= vy[i])
                {
                    return i;
                }
                val -= vy[i];
            }
            throw new Exception("Not in dictionary!");
        }

        private void SetLearningRate(double learningRate)
        {
            foreach (var layer in layers)
            {
                layer.LearningRate = learningRate;
            }
        }

        public override string ToString()
        {
            string layerDescription = $"{layers[0]}";
            foreach (var layer in layers.Skip(1))
            {
                layerDescription += "=" + $"{layer}";
            }
            return layerDescription;
        }

        public delegate void LearningHandler(object source, LearningEvent e);
        public event LearningHandler Learning;
        public class LearningEvent
        {
            public LearningEvent(double progress, int currentIteration, double learningRate)
            {
                Progress = progress;
                CurrentIteration = currentIteration;
                LearningRate = learningRate;
            }
            public double Progress { get; set; }
            public int CurrentIteration { get; set; }
            public double LearningRate { get; set; }
        }

        public delegate void GeneratingHandler(object source, GeneratingEvent e);
        public event GeneratingHandler Generating;
        public class GeneratingEvent
        {
            public GeneratingEvent(double progress)
            {
                Progress = progress;
            }
            public double Progress { get; set; }
            public int CurrentIteration { get; set; }
            public double GeneratingRate { get; set; }
        }
    }
}