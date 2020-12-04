using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using CNLab4;
using System.Collections;
using CNLab4.Messages.Server;
using CNLab4.Messages.Server.Requests;
using CNLab4.Messages.Server.Responses;

namespace CNLab4_Server
{
    class UnknownRequestException : Exception { }

    public class Server
    {
        private TcpListener _listener;
        private bool _isStarted = false;
        private Dictionary<string, TorrentInfo> _torrents = new Dictionary<string, TorrentInfo>();
        private PeersContainer _peersContainer = new PeersContainer();

        public Server(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public async void StartAsync()
        {
            if (_isStarted)
                return;
            _isStarted = true;

            _listener.Start();

            while (_isStarted)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                OnClientAccepted(client);
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;
            _isStarted = false;
        }

        private void OnClientAccepted(TcpClient client)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();

                    BaseServerRequest request = stream.ReadMessage<BaseServerRequest>();
                    try
                    {
                        BaseServerResponse response = OnRequest(request);
                        stream.Write(response);
                    }
                    catch (UnknownRequestException)
                    {
                        stream.Write(new Error { Text = "Unknown request." });
                    }
                }
            }
            catch { }
        }

        private BaseServerResponse OnRequest(BaseServerRequest request)
        {
            if (request is RegisterTorrent registerTorrent)
                return OnRequest(registerTorrent);
            else if (request is GetTorrentInfo getTorrentInfo)
                return OnRequest(getTorrentInfo);
            else if (request is GetPeersInfo getPeersInfo)
                return OnRequest(getPeersInfo);
            else if (request is BecomePeer becomePeer)
                return OnRequest(becomePeer);
            else
                throw new UnknownRequestException();
        }

        private BaseServerResponse OnRequest(RegisterTorrent request)
        {
            int filesCount = request.FilesInfo.Count;

            TorrentFileInfo[] torrentFilesInfo = new TorrentFileInfo[filesCount];
            int[] blockSizes = new int[filesCount];
            for (int i = 0; i < filesCount; ++i)
            {
                var fileInfo = request.FilesInfo[i];
                blockSizes[i] = CalcBlockSize(fileInfo.Length);
                torrentFilesInfo[i] = new TorrentFileInfo(fileInfo.RelativePath, fileInfo.Length, blockSizes[i]);
            }

            string accessCode = GenerateUniqueAccessCode(40);
            TorrentInfo torrentInfo = new TorrentInfo(request.TorrentName, torrentFilesInfo, accessCode);

            _torrents.Add(accessCode, torrentInfo);
            PeerInfo peerInfo = new PeerInfo(torrentInfo, request.SenderAddress);
            peerInfo.SetAllBlocksDone();
            _peersContainer.Add(peerInfo);

            return new TorrentRegistered
            {
                AccessCode = accessCode,
                BlocksSizes = blockSizes
            };
        }

        private BaseServerResponse OnRequest(GetTorrentInfo request)
        {
            if (_torrents.TryGetValue(request.AccessCode, out TorrentInfo torrentInfo))
            {
                return new TorrentInfoResponse { TorrentInfo = torrentInfo };
            }
            else
            {
                return new Error { Text = "Wrong access code. " };
            }
        }

        private BaseServerResponse OnRequest(GetPeersInfo request)
        {
            if (!_torrents.TryGetValue(request.AccessCode, out TorrentInfo torrentInfo))
                return new Error { Text = "Wrong access code." };
            if (request.NeedMasks.Count != torrentInfo.FilesInfo.Count)
                return new Error { Text = "Wrong count of files masks." };

            if (_peersContainer.TryGet(request.AccessCode, out IEnumerable<PeerInfo> infos))
            {
                List<PeersInfoResponse.Info> resultInfos = new List<PeersInfoResponse.Info>();
                foreach (PeerInfo peerInfo in infos)
                {
                    if (request.SenderAddress.Equals(peerInfo.Address))
                        continue;

                    bool isAnyTrue = false;
                    BitArray[] resultMasks = new BitArray[peerInfo.FilesState.Length];
                    for (int i = 0; i < peerInfo.FilesState.Length; ++i)
                    {
                        resultMasks[i] = (BitArray)request.NeedMasks[i].Clone();
                        resultMasks[i].And(peerInfo.FilesState[i]);
                        isAnyTrue |= resultMasks[i].AtLeastOne(true);
                    }

                    if (isAnyTrue)
                    {
                        resultInfos.Add(new PeersInfoResponse.Info
                        {
                            Address = peerInfo.Address,
                            FilesMasks = resultMasks
                        });
                    }  
                }

                return new PeersInfoResponse { Infos = resultInfos };
            }
            else
            {
                return new PeersInfoResponse { Infos = Array.Empty<PeersInfoResponse.Info>() };
            }
        }

        private BaseServerResponse OnRequest(BecomePeer request)
        {
            if (!_torrents.TryGetValue(request.AccessCode, out TorrentInfo torrentInfo))
                return new Error { Text = "Wrong access code." };

            if (!_peersContainer.TryGet(request.AccessCode, request.Address, out PeerInfo peerInfo))
            {
                peerInfo = new PeerInfo(torrentInfo, request.Address);
                _peersContainer.Add(peerInfo);
            }

            foreach (var block in request.AvailableBlocks)
            {
                peerInfo.SetBlockDone(block.FileIndex, block.BlockIndex);
            }
            return new Ok();
        }

        private string GenerateUniqueAccessCode(int minLength)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < minLength || _torrents.ContainsKey(builder.ToString()); ++i)
            {
                int value = random.Next(0, 62);
                if (value < 26)
                    builder.Append((char)('a' + value));
                else if (value < 52)
                    builder.Append((char)('A' + value - 26));
                else
                    builder.Append((char)('0' + value - 52));
            }
            return builder.ToString();
        }

        // static
        private static int CalcBlockSize(long fileLength)
        {
            return 4_000_000;

            //int minBlockSize = 10_000;
            //int maxBlockSize = 10_000_000;
            //int defaultBlocksCount = 100;

            //long blockSize = fileLength / defaultBlocksCount;
            //if (blockSize < minBlockSize)
            //    blockSize = minBlockSize;
            //else if (blockSize > maxBlockSize)
            //    blockSize = maxBlockSize;

            //return (int)blockSize;
        }
    }

}
