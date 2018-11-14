using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot
{
    public interface IInterpreter<T>
    {
        int Encode(T entity);
        T Decode(int code);
        List<T> CleanUp(IEnumerable<T> list);
        int VocabularySize { get; }
    }
}
