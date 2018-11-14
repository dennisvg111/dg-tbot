using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.Interpreters
{
    public class StaticDictionaryInterpreter : IInterpreter<char>
    {
        private string text;
        private char[] encode;
        public StaticDictionaryInterpreter(string text)
        {
            encode = text.Distinct().OrderBy(c => c).ToArray();
        }
        public int VocabularySize
        {
            get
            {
                return encode.Length;
            }
        }

        public List<char> CleanUp(IEnumerable<char> list)
        {
            return list.Where(c => encode.Contains(c)).ToList();
        }

        public char Decode(int code)
        {
            return encode[code];
        }

        public int Encode(char entity)
        {
            return encode.ToList().IndexOf(entity);
        }
    }
}
