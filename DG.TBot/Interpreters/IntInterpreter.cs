using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.Interpreters
{
    public class IntInterpreter : IInterpreter<int>
    {
        int min;
        int max;
        public IntInterpreter(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int VocabularySize
        {
            get
            {
                return max - min + 1;
            }
        }

        public List<int> CleanUp(IEnumerable<int> list)
        {
            return list.Where(i => i >= min && i <= max).ToList();
        }

        public int Decode(int code)
        {
            return code + min;
        }

        public int Encode(int entity)
        {
            return entity - min;
        }
    }
}
