using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    [JsonConverter(typeof(TorrentFileInfoConverter))]
    public class TorrentFileInfo
    {
        public string FilePath { get; private set; }
        public long FileLength { get; private set; }
        public int BlockSize { get; private set; }
        public int BlocksCount { get; private set; }
        public int LastBlockSize { get; private set; }

        public TorrentFileInfo(string path, long fileLength, int blockSize)
        {
            if (Path.IsPathRooted(path))
                throw new ArgumentException("Path of file is rooted.");

            FilePath = path;
            FileLength = fileLength;
            BlockSize = blockSize;

            LastBlockSize = (int)(fileLength % blockSize);
            if (LastBlockSize == 0)
            {
                BlocksCount = (int)(fileLength / blockSize);
                LastBlockSize = blockSize;
            }
            else
            {
                BlocksCount = (int)(fileLength / blockSize + 1);
            }
        }

        public int GetBlockSize(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= BlocksCount)
                throw new ArgumentException("Wrong block index.");

            if (blockIndex == BlocksCount - 1)
                return LastBlockSize;
            else
                return BlockSize;
        }
    }

}
