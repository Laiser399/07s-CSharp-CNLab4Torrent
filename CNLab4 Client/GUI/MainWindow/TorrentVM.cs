using CNLab4;
using CNLab4.Messages;
using CNLab4.Messages.Peer;
using CNLab4.Messages.Peer.Requests;
using CNLab4.Messages.Peer.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CNLab4_Client.GUI
{
    public class TorrentVM : BaseViewModel
    {
        private TorrentInfo _torrentInfo;
        private Speedometer _receiveSpeedometer = new Speedometer(10_000);
        private Speedometer _sendSpeedometer = new Speedometer(10_000);

        #region Bindings

        private string _directory;
        public string Directory => _directory;

        public string AccessCode => _torrentInfo.AccessCode;

        public string Name => _torrentInfo.Name;

        private TorrentFileVM[] _files;
        public IReadOnlyList<TorrentFileVM> Files => _files;

        public string ProgressStrRepr => ((double)_received / FullSize * 100).ToString("F2") + '%';

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

        public long FullSize => _torrentInfo.FullSize;
        public string FullSizeStrRepr => General.GetSizeStrRepr(FullSize);

        public string ReceiveSpeedStrRepr => _receiveSpeedometer.GetSpeedStrRepr();
        public string SendSpeedStrRepr => _sendSpeedometer.GetSpeedStrRepr();

        #endregion

        public TorrentVM(string baseDirectory, TorrentInfo torrentInfo, bool isDone = false)
        {
            _torrentInfo = torrentInfo;
            _directory = baseDirectory;
            _files = torrentInfo.FilesInfo.Select(fInfo => new TorrentFileVM(Directory, fInfo, isDone)).ToArray();

            if (isDone)
            {
                _received = FullSize;
            }
            else
            {
                ReceiveUntilDoneAsync();
            }

        }

        public void AddBytesSent(long bytesCount)
        {
            _sendSpeedometer.Add(bytesCount);
        }

        public void UpdateSpeeds()
        {
            NotifyPropChanged(nameof(ReceiveSpeedStrRepr), nameof(SendSpeedStrRepr));
        }

        private async void ReceiveUntilDoneAsync()
        {
            List<IPEndPoint> connectedPeers = new List<IPEndPoint>();
            List<Task> receiveTasks = new List<Task>();
            int maxPeersCount = 10;
            long lastRequestTime = 0;
            int delay = 10_000;

            while (Received < FullSize)
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long toWait = lastRequestTime + delay - now;
                if (toWait > 0)
                    await Task.Delay((int)toWait);

                // receive peers
                IList<IPEndPoint> peers = await GetPeersAsync();
                lastRequestTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (peers is null)
                    continue;

                for (int i = 0; i < peers.Count; ++i)
                {
                    IPEndPoint peer = peers[i];
                    if (peer.Equals(General.PeerAddress))
                        continue;
                    if (connectedPeers.Contains(peer))
                        continue;

                    if (connectedPeers.Count >= maxPeersCount)
                    {
                        Task doneTask = await Task.WhenAny(receiveTasks);
                        receiveTasks.Remove(doneTask);
                    }

                    connectedPeers.Add(peer);
                    Task task = ReceiveFromAsync(peer).ContinueWith(_ => connectedPeers.Remove(peer));
                    receiveTasks.Add(task);
                }


            }
        }

        private async Task ReceiveFromAsync(IPEndPoint peer)
        {
            try
            {
                using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
                {
                    int timeout = 5000;
                    if (!await client.TryConnectAsync(peer, timeout))
                    {
                        General.Log($"Could not connect to {peer} in {timeout}ms.");
                        return;
                    }
                    NetworkStream stream = client.GetStream();
#if TASKRUN
                    await stream.WriteTask(new GetDoneMasks { AccessCode = AccessCode });
                    BasePeerResponse response = await stream.ReadMessageTask<BasePeerResponse>();
#else
                    await stream.WriteAsync(new GetDoneMasks { AccessCode = AccessCode });
                    BasePeerResponse response = await stream.ReadMessageAsync<BasePeerResponse>();
#endif
                    if (response is DoneMasksResponse doneMasksResponse)
                    {
                        if (IsValidMasks(doneMasksResponse.DoneMasks))
                        {
                            await ReceiveBlocksAsync(stream, doneMasksResponse.DoneMasks);
                        }
                    }
                    else if (response is Error errorResponse)
                    {
                        General.Log("Error on receive done masks",
                            $"\tMessage: {errorResponse.Text}",
                            $"\tPeer address: {peer}");
                    }
                    else
                    {
                        General.Log($"Unknown response on {nameof(GetDoneMasks)} request",
                            $"\tPeer address: {peer}");
                    }
                }
            }
            catch (Exception e)
            {
                General.Log("Receive done with error",
                    $"\tMessage: {e.Message}",
                    $"\tPeer address: {peer}");
            }
        }

        private bool IsValidMasks(IList<BitArray> masks)
        {
            if (masks.Count != Files.Count)
                return false;
            for (int i = 0; i < masks.Count; ++i)
                if (Files[i].BlocksCount != masks[i].Length)
                    return false;
            return true;
        }
        
        private async Task ReceiveBlocksAsync(NetworkStream stream, IList<BitArray> peerDone)
        {
            List<Block> blocks = GetBlocksToReceive(peerDone);

            Random random = new Random();
            while (blocks.Count > 0 && Received < FullSize)
            {
                int index = random.Next(blocks.Count);
                Block block = blocks[index];
                blocks.RemoveAt(index);
                if (!Files[block.FileIndex].IsDone(block.BlockIndex))
                    if (!await ReceiveBlockAsync(stream, block.FileIndex, block.BlockIndex))
                        return;
            }
        }

        private List<Block> GetBlocksToReceive(IList<BitArray> peerDone)
        {
            BitArray[] canReceive = GetUndoneMasks();
            for (int i = 0; i < canReceive.Length; ++i)
                canReceive[i].And(peerDone[i]);

            return GetTrueBlocks(canReceive);
        }

        private List<Block> GetTrueBlocks(IEnumerable<BitArray> masks)
        {
            List<Block> blocks = new List<Block>();
            int i = 0;
            foreach (BitArray mask in masks)
            {
                for (int j = 0; j < mask.Length; ++j)
                    if (mask[j])
                        blocks.Add(new Block { FileIndex = i, BlockIndex = j });
                i++;
            }
            return blocks;
        }

        private async Task<bool> ReceiveBlockAsync(NetworkStream stream, int fIndex, int bIndex)
        {
#if TASKRUN
            await stream.WriteTask(new GetBlock
            {
                AccessCode = AccessCode,
                FileIndex = fIndex,
                BlockIndex = bIndex
            });
            BasePeerResponse response = await stream.ReadMessageTask<BasePeerResponse>();
#else
            await stream.WriteAsync(new GetBlock
            {
                AccessCode = AccessCode,
                FileIndex = fIndex,
                BlockIndex = bIndex
            });
            BasePeerResponse response = await stream.ReadMessageAsync<BasePeerResponse>();
#endif
            if (response is BlockSentResponse)
            {
                int blockSize = Files[fIndex].GetBlockSize(bIndex);
#if TASKRUN
                byte[] data = await stream.ReadBytesTask(blockSize);
#else
                byte[] data = await stream.ReadBytesAsync(blockSize);
#endif
                if (await Files[fIndex].TryWriteAsync(bIndex, data))
                    OnBlockReceived(data.Length);
                return true;
            }
            else if (response is Error errorResponse)
            {
                General.Log("Error response on receiving blocks",
                    $"\tMessage: {errorResponse.Text}");
                return false;
            }
            else
            {
                General.Log("Unknown response on receiving blocks");
                return false;
            }
        }

        private async Task<IList<IPEndPoint>> GetPeersAsync()
        {
            try
            {
#if TASKRUN
                return await ServerProtocol.GetPeersTask(AccessCode);
#else
                return await ServerProtocol.GetPeersAsync(AccessCode);
#endif
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

        private void OnBlockReceived(int blockSize)
        {
            Received += blockSize;
            _receiveSpeedometer.Add(blockSize);
        }

        public void OnBlockSent(int blockSize)
        {
            _sendSpeedometer.Add(blockSize);
        }

        public BitArray[] GetDoneMasks()
        {
            BitArray[] doneMasks = new BitArray[Files.Count];
            for (int i = 0; i < Files.Count; ++i)
            {
                doneMasks[i] = Files[i].DoneMask;
            }
            return doneMasks;
        }


    }
}
