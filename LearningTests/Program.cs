using DG.TBot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningTests
{
    class Program
    {
        private static ProgressBar progress;
        private static PerformanceCounter theCPUCounter;

        private static DateTime start;
        static void Main(string[] args)
        {
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

            var interpreter = new DG.TBot.Interpreters.StaticDictionaryInterpreter(text);
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

        private static void Learner_Generating(object source, LSTMLearner<char>.GeneratingEvent e)
        {
            if (progress != null)
            {
                progress.Report(e.Progress);
            }
        }

        public static void Learner_Learning(object o, LSTMLearner<char>.LearningEvent e)
        {
            if (progress != null)
            {
                progress.Report(e.Progress);
            }
        }
    }
}
