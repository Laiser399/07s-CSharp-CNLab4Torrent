using CNLab4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4_Server
{
    class PeersContainer
    {
        private Dictionary<PeerInfo.Key, PeerInfo> _peerInfos = new Dictionary<PeerInfo.Key, PeerInfo>();
        private Dictionary<IPEndPoint, HashSet<PeerInfo>> _byAddress = new Dictionary<IPEndPoint, HashSet<PeerInfo>>();
        private Dictionary<string, HashSet<PeerInfo>> _byAccessCode = new Dictionary<string, HashSet<PeerInfo>>();

        public bool TryGet(TorrentInfo torrentInfo, out IEnumerable<PeerInfo> infos)
        {
            return TryGet(torrentInfo.AccessCode, out infos);
        }

        public bool TryGet(string accessCode, out IEnumerable<PeerInfo> infos)
        {
            if (_byAccessCode.TryGetValue(accessCode, out HashSet<PeerInfo> tm))
            {
                infos = tm;
                return true;
            }
            else
            {
                infos = null;
                return false;
            }
        }

        public bool TryGet(TorrentInfo torrentInfo, IPEndPoint address, out PeerInfo info)
        {
            return TryGet(torrentInfo.AccessCode, address, out info);
        }

        public bool TryGet(string accessCode, IPEndPoint address, out PeerInfo peerInfo)
        {
            PeerInfo.Key key = new PeerInfo.Key(accessCode, address);
            return _peerInfos.TryGetValue(key, out peerInfo);
        }

        public void Add(TorrentInfo torrentInfo, IPEndPoint address)
        {
            PeerInfo peerInfo = new PeerInfo(torrentInfo, address);
            Add(peerInfo);
        }

        public void Add(PeerInfo peerInfo)
        {
            if (_peerInfos.ContainsKey(peerInfo.GetKey()))
                return;

            _peerInfos.Add(peerInfo.GetKey(), peerInfo);

            if (_byAddress.TryGetValue(peerInfo.Address, out HashSet<PeerInfo> hashSet))
            {
                hashSet.Add(peerInfo);
            }
            else
            {
                hashSet = new HashSet<PeerInfo>();
                hashSet.Add(peerInfo);
                _byAddress.Add(peerInfo.Address, hashSet);
            }

            if (_byAccessCode.TryGetValue(peerInfo.AccessCode, out hashSet))
            {
                hashSet.Add(peerInfo);
            }
            else
            {
                hashSet = new HashSet<PeerInfo>();
                hashSet.Add(peerInfo);
                _byAccessCode.Add(peerInfo.AccessCode, hashSet);
            }
        }
    }

}
