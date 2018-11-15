using DG.TBot;
using DG.TBot.Interpreters;
using DG.TBot.LSTM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace LearningTests
{
    class Program
    {
        private static ProgressBar progress;
        private static PerformanceCounter theCPUCounter;

        private static DateTime start;
        static void Main(string[] args)
        {
            var odd = new DG.TBot.ENN.ENNLearner<int, bool>(new IntInterpreter(0, 5), new BoolInterpreter(), 1, 1);
            for (int i = 0; i < 125; i++)
            {
                odd.AddTrainingData(new DG.TBot.ENN.TrainingData<int, bool>(new int[] { i }, new bool[] { i % 2 != 0 }));
            }

            odd.Learning += Odd_Learning;

            for (int i = 0; i < 500; i++)
            {
                float processorUsage;
                using (progress = new ProgressBar("Learning"))
                {
                    odd.Evolve(1, 100);
                    processorUsage = theCPUCounter == null ? 0 : theCPUCounter.NextValue() / Environment.ProcessorCount;
                }
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write($"[{(DateTime.Now - start).ToString(@"hh\:mm\:ss")}] iteration: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{odd.Generation}");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write($", delta: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{odd.Delta:N0}");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write($", learning rate: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{odd.LearningRate:0.000}");
                Console.ResetColor();
            }

            Console.WriteLine($"1 is odd: {odd.Ask(1)[0]}");
            Console.WriteLine($"2 is odd: {odd.Ask(2)[0]}");
            Console.WriteLine($"3 is odd: {odd.Ask(3)[0]}");
            Console.WriteLine($"111 is odd: {odd.Ask(111)[0]}");
            Console.WriteLine($"128 is odd: {odd.Ask(128)[0]}");
            Console.WriteLine();

            start = DateTime.Now;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string text;
            using (var sr = new StreamReader("test.txt"))
            {
                text = sr.ReadToEnd();
            }

            try
            {
                theCPUCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            }
            catch (Exception)
            {
                theCPUCounter = null;
            }

            var interpreter = new CharInterpreter();
            LSTMLearner<char> learner = new LSTMLearner<char>(text.ToCharArray(), interpreter);
            learner.Learning += Learner_Learning;
            learner.Generating += Learner_Generating;
            
            Console.WriteLine($"Created learner: {learner}\r\n  with {learner.Parameters.ToString("N0")} parameters");
            Console.WriteLine();
            theCPUCounter?.NextValue();

            while (true)
            {
                for (int i = 0; i < 5; i++)
                {
                    float processorUsage;
                    using (progress = new ProgressBar("Learning"))
                    {
                        learner.Learn(1);
                        processorUsage = theCPUCounter == null ? 0 : theCPUCounter.NextValue() / Environment.ProcessorCount;
                    }
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($"[{(DateTime.Now- start).ToString(@"hh\:mm\:ss")}] iteration: ");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write($"{learner.Iteration}");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($", estimated loss: ");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write($"{learner.EstimatedLoss:0.000}");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($", CPU usage(avg): ");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"{processorUsage:0.0}%");
                    Console.ResetColor();
                }
                List<char> output = null;
                using (progress = new ProgressBar("Generating text"))
                {
                    output = learner.Generate(500, "Sirrah, ");
                }
                Console.WriteLine();
                Console.WriteLine(string.Concat(output));
                Console.WriteLine();
            }
        }

        private static void Odd_Learning(object source, LearningEvent e)
        {
            if (progress != null)
            {
                progress.Report(e.Progress);
            }
        }

        private static void Learner_Generating(object source, LSTMLearner<char>.GeneratingEvent e)
        {
            if (progress != null)
            {
                progress.Report(e.Progress);
            }
        }

        public static void Learner_Learning(object o, LearningEvent e)
        {
            if (progress != null)
            {
                progress.Report(e.Progress);
            }
        }
    }
}
