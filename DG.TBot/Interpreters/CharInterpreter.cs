using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.Interpreters
{
    public class CharInterpreter : IInterpreter<char>
    {
        public int VocabularySize
        {
            get
            {
                return 128;
            }
        }

        public List<char> CleanUp(IEnumerable<char> list)
        {
            return list.Where(c => (int)c >= 0 && (int)c < 128).ToList();
        }

        public char Decode(int code)
        {
            return (char)code;
        }

        public int Encode(char entity)
        {
            return (int)entity;
        }
    }
}
