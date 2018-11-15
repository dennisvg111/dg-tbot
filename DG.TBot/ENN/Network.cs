using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.TBot.ENN
{
    internal static class NetworkUtilities
    {
        public static List<ENNLayer> CreateOffspring(this List<ENNLayer> layers)
        {
            List<ENNLayer> clonedList = new List<ENNLayer>();
            foreach (var layer in layers)
            {
                var clonedLayer = layer.Clone();
                clonedLayer.Mutate();
                clonedList.Add(clonedLayer);
            }
            return clonedList;
        }
    }
}
