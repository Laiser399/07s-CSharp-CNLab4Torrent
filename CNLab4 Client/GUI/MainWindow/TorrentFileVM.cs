using CNLab4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4_Client.GUI
{
    public class TorrentFileVM : BaseViewModel
    {

        #region Bindings

        private string _relativePath;
        public string RelativePath => _relativePath;

        private string _fullPath;
        public string FullPath => _fullPath;

        private long _received = 0;
        public long Received
        {
            get => _received;
            set
            {
                _received = value;
                NotifyPropChanged(nameof(Received), nameof(ProgressStrRepr));
            }
        }
        public string ProgressStrRepr => ((double)Received / FileSize * 100).ToString("F2") + '%';

        private long _fileSize;
        public long FileSize => _fileSize;
        public string FileSizeStrRepr => General.GetSizeStrRepr(FileSize);

        private int _blocksCount;
        public int BlocksCount => _blocksCount;

        private int _blockSize;
        public int BlockSize => _blockSize;

        private int _lastBlockSize;
        public int LastBlockSize => _lastBlockSize;

        #endregion

        private BitArray _doneMask;

        public TorrentFileVM(string directory, TorrentFileInfo fileInfo, bool isDone = false)
        {
            _relativePath = fileInfo.FilePath;
            _fullPath = Path.Combine(directory, _relativePath);
            _fileSize = fileInfo.FileLength;
            _blocksCount = fileInfo.BlocksCount;
            _blockSize = fileInfo.BlockSize;
            _lastBlockSize = fileInfo.LastBlockSize;

            if (isDone)
            {
                _doneMask = new BitArray(_blocksCount, true);
                _received = FileSize;
            }
            else
            {
                _doneMask = new BitArray(_blocksCount, false);
            }
        }

        public void CreateEmptyFile()
        {
            FileInfo fileInfo = new FileInfo(FullPath);
            Directory.CreateDirectory(fileInfo.DirectoryName);// TODO exc
            using (FileStream fStream = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fStream.SetLength(FileSize);
            }
        }

        public bool IsDone(int blockIndex)
        {
            return _doneMask[blockIndex];
        }

        public async Task<byte[]> ReadAsync(int blockIndex)
        {
            using (FileStream fStream = File.Open(FullPath, FileMode.Open, FileAccess.Read))
            {
                fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                return await fStream.ReadBytesAsync(GetBlockSize(blockIndex));
            }
        }

        public byte[] Read(int blockIndex)
        {
            using (FileStream fStream = File.Open(FullPath, FileMode.Open, FileAccess.Read))
            {
                fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                return fStream.ReadBytes(GetBlockSize(blockIndex));
            }
        }

        public async Task WriteAsync(int blockIndex, byte[] data)
        {
            if (IsDone(blockIndex))
                throw new ArgumentException("Block already done.");
            int currentBlockSize = GetBlockSize(blockIndex);
            if (data.Length != currentBlockSize)
                throw new ArgumentException("Wrong length of bytes array.");

            await Task.Run(() =>
            {
                using (FileStream fStream = File.Open(FullPath, FileMode.Open))
                {
                    fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                    fStream.Write(data, 0, currentBlockSize);
                }
            });
            //using (FileStream fStream = File.Open(FullPath, FileMode.Open))
            //{
            //    fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
            //    await fStream.WriteAsync(data, 0, currentBlockSize);
            //}

            Received += data.Length;
            _doneMask.Set(blockIndex, true);
        }

        public void Write(int blockIndex, byte[] data)
        {
            if (IsDone(blockIndex))
                throw new ArgumentException("Block already done.");
            int currentBlockSize = GetBlockSize(blockIndex);
            if (data.Length != currentBlockSize)
                throw new ArgumentException("Wrong length of bytes array.");

            using (FileStream fStream = File.Open(FullPath, FileMode.Open))
            {
                fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                fStream.Write(data, 0, currentBlockSize);
            }

            Received += data.Length;
            _doneMask.Set(blockIndex, true);
        }

        public int GetBlockSize(int blockIndex)
        {
            if (blockIndex == BlocksCount - 1)
                return LastBlockSize;
            else
                return BlockSize;
        }

        public BitArray GetUndoneMask()
        {
            BitArray undone = (BitArray)_doneMask.Clone();
            return undone.Not();
        }
    }
}
