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
        private Dictionary<string, HashSet<IPEndPoint>> _peers = new Dictionary<string, HashSet<IPEndPoint>>();
        //private PeersContainer _peersContainer = new PeersContainer();

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
            else if (request is GetPeers getPeers)
                return OnRequest(getPeers);
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
            AddPeer(accessCode, request.SenderAddress);

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
                AddPeer(request.AccessCode, request.SenderAddress);
                return new TorrentInfoResponse { TorrentInfo = torrentInfo };
            }
            else
            {
                return new Error { Text = "Wrong access code. " };
            }
        }

        private BaseServerResponse OnRequest(GetPeers request)
        {
            if (!_torrents.ContainsKey(request.AccessCode))
                return new Error { Text = "Wrong access code." };

            if (_peers.TryGetValue(request.AccessCode, out HashSet<IPEndPoint> peers))
            {
                return new PeersResponse { Peers = peers.ToArray() };
            }
            else
            {
                return new PeersResponse { Peers = Array.Empty<IPEndPoint>() };
            }
        }

        private void AddPeer(TorrentInfo torrent, IPEndPoint peer)
        {
            AddPeer(torrent.AccessCode, peer);
        }

        private void AddPeer(string accessCode, IPEndPoint peer)
        {
            if (_peers.TryGetValue(accessCode, out HashSet<IPEndPoint> peers))
            {
                if (!peers.Contains(peer))
                    peers.Add(peer);
            }
            else
            {
                peers = new HashSet<IPEndPoint>();
                peers.Add(peer);
                _peers.Add(accessCode, peers);
            }
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
