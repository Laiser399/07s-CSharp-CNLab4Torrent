using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4_Client
{
    static class DirectoryEx
    {
        public static List<string> GetRelativeFilesPaths(string dirPath)
        {
            List<string> result = new List<string>();
            Recursion(dirPath, "", result);
            return result;
        }

        private static void Recursion(string basePath, string relativePath, List<string> result)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(basePath, relativePath));
            foreach (FileInfo fInfo in dirInfo.GetFiles())
            {
                result.Add(Path.Combine(relativePath, fInfo.Name));
            }

            foreach (DirectoryInfo subdirInfo in dirInfo.GetDirectories())
                Recursion(basePath, Path.Combine(relativePath, subdirInfo.Name), result);
        }
    }
}
