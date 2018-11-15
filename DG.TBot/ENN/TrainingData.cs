using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.ENN
{
    public class TrainingData<T, V>
    {
        public T[] Input { get; set; }
        public V[] ExpectedOutput { get; set; }

        public TrainingData(T[] input, V[] expectedOutput)
        {
            Input = input;
            ExpectedOutput = expectedOutput;
        }
    }
}
