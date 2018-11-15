using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot
{
    internal static class Utilities
    {
        private static Random rnd = new Random();
        private static readonly object syncLock = new object();

        public static int[] ToBitArray(this int i)
        {
            BitArray b = new BitArray(new int[] { i });
            return b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
        }

        public static int ToInt(this int[] bits)
        {
            BitArray bitArray = new BitArray(bits.Select(b => b == 1).ToArray());
            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        public static bool RandomBool(double chance)
        {
            lock (syncLock)
            { // synchronize
                return rnd.NextDouble() < chance;
            }
        }
    }

    public delegate void LearningHandler(object source, LearningEvent e);
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
}
