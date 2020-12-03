using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public static class ConvertEx
    {
        public static string ToString(byte[] data)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = data.Length - 1; i > -1; --i)
            {
                builder.Append(Convert.ToString(data[i], 16).PadLeft(2, '0'));
            }
            return builder.ToString();
        }

        public static byte[] ToBytesArray(string hexStr)
        {
            if (hexStr.Length % 2 == 1)
                hexStr = '0' + hexStr;

            int bytesCount = hexStr.Length / 2;
            byte[] result = new byte[bytesCount];
            for (int i = 0; i < bytesCount; ++i)
            {
                string subStr = hexStr.Substring(hexStr.Length - i * 2 - 2, 2);
                result[i] = Convert.ToByte(subStr, 16);
            }
            return result;
        }
    }

}
