using CNLab4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CNLab4_Client.GUI
{
    public class TorrentFileVM : BaseViewModel, IDisposable
    {
        private TorrentFileInfo _torrentFileInfo;

        #region Bindings

        public string RelativePath => _torrentFileInfo.FilePath;
        public int BlocksCount => _torrentFileInfo.BlocksCount;
        public int BlockSize => _torrentFileInfo.BlockSize;
        public int LastBlockSize => _torrentFileInfo.LastBlockSize;

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

        public long FileSize => _torrentFileInfo.FileLength;
        public string FileSizeStrRepr => General.GetSizeStrRepr(FileSize);

        #endregion

        private SemaphoreSlim _slim = new SemaphoreSlim(1);
        private FileStream _fileStream;

        public bool Exists => File.Exists(FullPath);

        private BitArray _doneMask;
        public BitArray DoneMask => _doneMask;

        public TorrentFileVM(string directory, TorrentFileInfo fileInfo, bool isDone = false)
        {
            _torrentFileInfo = fileInfo;
            _fullPath = Path.Combine(directory, RelativePath);

            if (isDone)
            {
                _doneMask = new BitArray(BlocksCount, true);
                _received = FileSize;
            }
            else
            {
                _doneMask = new BitArray(BlocksCount, false);
            }
        }

        ~TorrentFileVM()
        {
            Dispose();
        }

        public bool IsDone(int blockIndex)
        {
            return _doneMask[blockIndex];
        }

        /// <returns>null if error</returns>
        public async Task<byte[]> TryReadAsync(int blockIndex)
        {
            try
            {
                await _slim.WaitAsync();

                if (!await TryOpenStreamAsync())
                    return null;
                return await Task.Run(() =>
                {
                    _fileStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                    return _fileStream.ReadBytes(GetBlockSize(blockIndex));
                });
            }
            catch
            {
                return null;
            }
            finally
            {
                _slim.Release();
            }
        }

        public async Task<bool> TryWriteAsync(int blockIndex, byte[] data)
        {
            try
            {
                await _slim.WaitAsync();

                if (IsDone(blockIndex))
                {
                    General.Log($"Block was not written cause it's done already.");
                    return false;
                }
                int currentBlockSize = GetBlockSize(blockIndex);
                if (data.Length != currentBlockSize)
                {
                    General.Log($"Block was not written cause it has wrong length.");
                    return false;
                }

                if (!await TryOpenStreamAsync())
                {
                    General.Log($"Block was not written cause failed to open file stream.");
                    return false;
                }

                await Task.Run(() =>
                {
                    _fileStream.Seek((long)BlockSize * blockIndex, SeekOrigin.Begin);
                    _fileStream.Write(data, 0, currentBlockSize);
                });
            }
            catch
            {
                return false;
            }
            finally
            {
                _slim.Release();
            }

            Received += data.Length;
            _doneMask.Set(blockIndex, true);
            if (Received == FileSize)
            {
                try
                {
                    await _slim.WaitAsync();
                    _fileStream.Close();
                    _fileStream = null;
                }
                finally
                {
                    _slim.Release();
                }
            }
            return true;
        }

        private async Task<bool> TryOpenStreamAsync()
        {
            try
            {
                if (!File.Exists(FullPath))
                    if (!await TryCreateEmptyFileAsync())
                        return false;

                if (_fileStream is object && !_fileStream.CanRead)
                {
                    _fileStream.Close();
                    _fileStream = null;
                }

                if (_fileStream is null)
                {
                    if (Received == FileSize)
                        _fileStream = File.OpenRead(FullPath);
                    else
                        _fileStream = File.Open(FullPath, FileMode.Open, FileAccess.ReadWrite);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TryCreateEmptyFileAsync()
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

        private bool TryCreateEmptyFile()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(FullPath);
                Directory.CreateDirectory(fileInfo.DirectoryName);
                using (FileStream fStream = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fStream.SetLength(FileSize);
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
