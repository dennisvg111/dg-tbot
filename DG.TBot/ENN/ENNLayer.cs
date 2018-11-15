using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.ENN
{
    internal class ENNLayer
    {
        private int size_output;
        private int size_input;
        private static readonly Random random = new Random();

        public double LearningRate { get; set; }

        private double[][] weights;
        private double[] bias;

        private ENNLayer(double[][] weights, double[] bias, double learningRate)
        {
            LearningRate = learningRate;
            size_output = bias.Length;
            size_input = weights.Length;
            this.weights = new double[size_input][];
            this.bias = new double[size_output];
            for (int i = 0; i < weights.Length; i++)
            {
                var neuron = weights[i];
                this.weights[i] = new double[neuron.Length];
                for (int o = 0; o < neuron.Length; o++)
                {
                    this.weights[i][o] = neuron[o];
                }
            }

            for (int o = 0; o < bias.Length; o++)
            {
                this.bias[o] = bias[o];
            }
        }

        public ENNLayer(int size_input, int size_output)
        {
            this.size_output = size_output;
            this.size_input = size_input;

            weights = new double[size_input][];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = new double[size_output];
                double[] neuron = weights[i];
                for (int o = 0; o < neuron.Length; o++)
                {
                    neuron[o] = RandomWeight() * 5;
                }
            }

            bias = new double[size_output];
            for (int o = 0; o < bias.Length; o++)
            {
                bias[o] = RandomWeight();
            }
        }

        public double[] Forward(double[] inputs)
        {
            double[] outputs = new double[size_output];
            for (int i = 0; i < weights.Length; i++)
            {
                double[] neuron = weights[i];
                double input = inputs[i];
                for (int o = 0; o < neuron.Length; o++)
                {
                    outputs[o] += input * neuron[o];
                }
            }

            for (int o = 0; o < size_output; o++)
            {
                double output = Sigmoid(outputs[o] + bias[o]);
                outputs[o] = output;
            }

            return outputs;
        }

        private double RandomWeight()
        {
            return (0.5 - random.NextDouble()) * 0.2;
        }

        private static double Sigmoid(double x)
        {
            return x < -45.0 ? 0.0 : x > 45.0 ? 1.0 : 1.0 / (1.0 + Math.Exp(-x));
        }

        private static double dSigmoid(double x)
        {
            return (1 - x) * x;
        }

        public ENNLayer Clone()
        {
            return new ENNLayer(weights, bias, LearningRate);
        }

        public void Mutate()
        {
            for (int i = 0; i < weights.Length; i++)
            {
                double[] neuron = weights[i];
                for (int o = 0; o < neuron.Length; o++)
                {
                    double sign = (random.NextDouble() * 2) - 1;
                    neuron[o] += RandomWeight() * LearningRate * sign;
                }
            }
        }
    }
}
