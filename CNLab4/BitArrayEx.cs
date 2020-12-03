using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public static class BitArrayEx
    {
        public static bool IsAll(this BitArray bitArray, bool value)
        {
            foreach (bool b in bitArray)
                if (b != value)
                    return false;
            return true;
        }

        public static bool AtLeastOne(this BitArray bitArray, bool value)
        {
            foreach (bool b in bitArray)
                if (b == value)
                    return true;
            return false;
        }

        public static List<int> GetIndicesOf(this BitArray bitArray, bool value)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < bitArray.Length; ++i)
                if (bitArray[i])
                    result.Add(i);
            return result;
        }
    }

}
