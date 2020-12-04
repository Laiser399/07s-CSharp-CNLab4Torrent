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
    public class TorrentFileVM : BaseViewModel, IDisposable
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

        private FileStream _fileStream;

        public bool Exists => File.Exists(FullPath);

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

        ~TorrentFileVM()
        {
            Dispose();
        }

        public async Task<bool> TryCreateEmptyFileAsync()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(FullPath);
                Directory.CreateDirectory(fileInfo.DirectoryName);
                await Task.Run(() =>
                {
                    using (FileStream fStream = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fStream.SetLength(FileSize);
                    }
                });
                
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        public bool IsDone(int blockIndex)
        {
            return _doneMask[blockIndex];
        }

        /// <returns>null if error</returns>
        public async Task<byte[]> TryReadAsync(int blockIndex)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!TryOpenStream())
                        return null;

                    lock (_fileStream)
                    {
                        _fileStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                        return _fileStream.ReadBytes(GetBlockSize(blockIndex));
                    }
                    //using (FileStream fStream = File.Open(FullPath, FileMode.Open, FileAccess.Read))
                    //{
                    //    fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                    //    return fStream.ReadBytes(GetBlockSize(blockIndex));
                    //}
                }
                catch
                {
                    return null;
                }
            });
        }

        public async Task<bool> TryWriteAsync(int blockIndex, byte[] data)
        {
            if (IsDone(blockIndex))
                return false;
            int currentBlockSize = GetBlockSize(blockIndex);
            if (data.Length != currentBlockSize)
                return false;

            bool res = await Task.Run(() =>
            {
                try
                {

                    if (!TryOpenStream())
                        return false;

                    lock (_fileStream)
                    {
                        _fileStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                        _fileStream.Write(data, 0, currentBlockSize);
                    }

                    //using (FileStream fStream = File.Open(FullPath, FileMode.Open))
                    //{
                    //    fStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                    //    fStream.Write(data, 0, currentBlockSize);
                    //}
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            if (res)
            {
                Received += data.Length;
                _doneMask.Set(blockIndex, true);
                if (Received == _fileSize)
                {
                    lock (_fileStream)
                    {
                        _fileStream.Close();
                        _fileStream = null;
                    }
                }
                return true;
            }
            else
                return false;
        }

        private bool TryOpenStream()
        {
            try
            {
                if (_fileStream is object)
                {
                    if (!_fileStream.CanRead)
                    {
                        _fileStream.Close();
                        _fileStream = null;
                    }
                }

                if (_fileStream is null)
                {
                    if (Received == _fileSize)
                        _fileStream = File.Open(FullPath, FileMode.Open, FileAccess.Read);
                    else
                        _fileStream = File.Open(FullPath, FileMode.Open);
                }
                return true;
            }
            catch
            {
                return false;
            }
            
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

        public virtual void Dispose()
        {
            if (_fileStream is object)
                _fileStream.Close();
        }
    }
}
