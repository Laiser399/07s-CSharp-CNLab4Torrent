using CNLab4;
using CNLab4.Messages;
using CNLab4.Messages.Peer;
using CNLab4.Messages.Peer.Requests;
using CNLab4.Messages.Peer.Responses;
using CNLab4.Messages.Server.Responses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CNLab4_Client.GUI
{
    public class TorrentVM : BaseViewModel
    {
        private string _directory;
        public string Directory => _directory;

        private Speedometer _sendSpeedometer = new Speedometer(10_000);

        #region Bindings

        private string _accessCode;
        public string AccessCode => _accessCode;

        private string _name;
        public string Name => _name;

        private TorrentFileVM[] _files;
        public IReadOnlyList<TorrentFileVM> Files => _files;

        public string ProgressStrRepr => ((double)_received / _fullSize * 100).ToString("F2") + '%';

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

        private long _fullSize;
        public string FullSizeStrRepr => General.GetSizeStrRepr(_fullSize);

        public string SendSpeedStrRepr => _sendSpeedometer.GetSpeedStrRepr();

        #endregion

        public TorrentVM(string baseDirectory, TorrentInfo torrentInfo, bool isDone = false)
        {
            _directory = baseDirectory;
            _accessCode = torrentInfo.AccessCode;
            _name = torrentInfo.Name;
            _files = torrentInfo.FilesInfo.Select(fInfo => new TorrentFileVM(Directory, fInfo, isDone)).ToArray();
            _fullSize = torrentInfo.GetFullSize();

            if (isDone)
            {
                _received = _fullSize;
            }
            else
            {
                CreateEmptyFiles();
                ReceiveUntilDone();
            }

            UpdateSpeedAsync();
        }

        private async void UpdateSpeedAsync()
        {
            while (true)
            {
                NotifyPropChanged(nameof(SendSpeedStrRepr));
                await Task.Delay(1000);
            }
        }

        private void CreateEmptyFiles()
        {
            foreach (TorrentFileVM file in Files)
            {
                file.CreateEmptyFile();
            }
        }

        public void AddBytesSent(long bytesCount)
        {
            _sendSpeedometer.Add(bytesCount);
        }

        private async void ReceiveUntilDone()
        {
            while (Received < _fullSize)
            {
                // receive peers
                IList<PeersInfoResponse.Info> infos = await GetPeersInfoAsync();
                if (infos is null || infos.Count == 0)
                {
                    await Task.Delay(10_000);
                    continue;
                }

                // receive data from each peer
                foreach (var peerInfo in infos)
                {
                    List<FileBlocks> blocksNeed = GetBlocksNeeded(peerInfo.FilesMasks);
                    if (blocksNeed.Count == 0)
                        continue;

                    using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
                    {
                        await client.ConnectAsync(peerInfo.Address.Address, peerInfo.Address.Port);
                        NetworkStream stream = client.GetStream();

                        await stream.WriteAsync(new GetBlocks
                        {
                            AccessCode = AccessCode,
                            BlocksNeed = blocksNeed
                        });

                        await ReadBlocksAsync(stream);
                    }
                }

            }
        }

        
        private async Task<IList<PeersInfoResponse.Info>> GetPeersInfoAsync()
        {
            BitArray[] undoneMasks = GetUndoneMasks();
            try
            {
                return await Task.Run(() => ServerProtocol.GetPeersInfo(AccessCode, undoneMasks));
                //return await ServerProtocol.GetPeersInfoAsync(AccessCode, undoneMasks);
            }
            catch (ErrorResponseException e)
            {
                General.Log(new string[]
                {
                    $"Server error response on retrieve peers info",
                    $"\tTorrent name: {Name}",
                    $"\tMessage: {e.Message}"
                });
                return null;
            }
            catch (UnknownResponseException)
            {
                General.Log(new string[]
                {
                    "Server unknown response on retrieve torrent info",
                    $"\tTorrent name: {Name}"
                });
                return null;
            }
            catch
            {
                General.Log(new string[]
                {
                    $"Unknown exception on retrieve torrent info",
                    $"\tTorrent name: {Name}"
                });
                return null;
            }
        }

        private BitArray[] GetUndoneMasks()
        {
            return Files.Select(file => file.GetUndoneMask()).ToArray();
        }

        private List<FileBlocks> GetBlocksNeeded(IList<BitArray> availableBlocks)
        {
            List<FileBlocks> blocksNeed = new List<FileBlocks>();

            for (int i = 0; i < Files.Count; ++i)
            {
                BitArray canRetrieve = Files[i].GetUndoneMask();
                canRetrieve.And(availableBlocks[i]);
                List<int> blocksIndices = canRetrieve.GetIndicesOf(true);

                if (blocksIndices.Count > 0)
                {
                    blocksNeed.Add(new FileBlocks
                    {
                        FileIndex = i,
                        BlocksIndices = blocksIndices
                    });
                }
            }

            return blocksNeed;
        }

        private async Task ReadBlocksAsync(NetworkStream stream)
        {
            while (true)
            {
                BasePeerResponse peerResponse = await Task.Run(() => stream.ReadMessage<BasePeerResponse>());
                //BasePeerResponse peerResponse = await stream.ReadMessageAsync<BasePeerResponse>();
                if (peerResponse is BlockResponse blockResponse)
                {
                    int fIndex = blockResponse.Block.FileIndex;
                    int bIndex = blockResponse.Block.BlockIndex;
                    TorrentFileVM file = Files[fIndex];
                    int blockSize = file.GetBlockSize(bIndex);

                    // read bytes
                    byte[] data = await Task.Run(() => stream.ReadBytes(blockSize));
                    //byte[] data = await stream.ReadBytesAsync(blockSize);

                    // write to file, mark as done
                    await Task.Run(() => file.Write(bIndex, data));
                    //await file.WriteAsync(bIndex, data);

                    // become peer
                    await BecomePeerAsync(fIndex, bIndex);

                    Received += data.Length;
                }
                else if (peerResponse is EndResponse)
                {
                    break;
                }
                else
                {
                    General.Log("Warning! Received unknown response from peer.");
                    break;
                }
            }
        }

        //private void ReadBlocks(NetworkStream stream)
        //{
        //    while (true)
        //    {
        //        BasePeerResponse peerResponse = stream.ReadMessage<BasePeerResponse>();
        //        if (peerResponse is BlockResponse blockResponse)
        //        {
        //            int fIndex = blockResponse.Block.FileIndex;
        //            int bIndex = blockResponse.Block.BlockIndex;
        //            TorrentFileVM file = Files[fIndex];
        //            int blockSize = file.GetBlockSize(bIndex);
        //            // read bytes
        //            byte[] data = stream.ReadBytes(blockSize);
        //            // write to file, mark as done
        //            file.Write(bIndex, data);
        //            // become peer
        //            await BecomePeerAsync(fIndex, bIndex);
        //        }
        //        else if (peerResponse is EndResponse)
        //        {
        //            break;
        //        }
        //        else
        //        {
        //            General.Log("Warning! Received unknown response from peer.");
        //            break;
        //        }
        //    }
        //}

        private async Task BecomePeerAsync(int fileIndex, int blockIndex)
        {
            try
            {
                await Task.Run(() =>
                {
                    ServerProtocol.BecomePeer(AccessCode, new Block
                    {
                        FileIndex = fileIndex,
                        BlockIndex = blockIndex
                    });
                });
                //await ServerProtocol.BecomePeerAsync(AccessCode, new Block
                //{
                //    FileIndex = fileIndex,
                //    BlockIndex = blockIndex
                //});
            }
            catch (ErrorResponseException e)
            {
                General.Log(new string[]
                {
                    $"Server error response on become peer",
                    $"\tTorrent name: {Name}",
                    $"\tMessage: {e.Message}"
                });
            }
            catch (UnknownResponseException)
            {
                General.Log(new string[]
                {
                    "Server unknown response on become peer",
                    $"\tTorrent name: {Name}"
                });
            }
            catch
            {
                General.Log(new string[]
                {
                    $"Unknown exception on become peer",
                    $"\tTorrent name: {Name}"
                });
            }
        }



    }
}
