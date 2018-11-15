using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.Interpreters
{
    public class BoolInterpreter : IInterpreter<bool>
    {
        public int VocabularySize
        {
            get
            {
                return 2;
            }
        }

        public List<bool> CleanUp(IEnumerable<bool> list)
        {
            return list.ToList();
        }

        public bool Decode(int code)
        {
            return code == 1;
        }

        public int Encode(bool entity)
        {
            return entity ? 1 : 0;
        }
    }
}
