using CNLab4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4_Server
{
    class PeerInfo
    {
        public string AccessCode { get; private set; }
        public IPEndPoint Address { get; private set; }
        public BitArray[] FilesState { get; private set; }

        public PeerInfo(TorrentInfo torrentInfo, IPEndPoint peerAddress)
        {
            AccessCode = torrentInfo.AccessCode;
            Address = peerAddress;
            FilesState = new BitArray[torrentInfo.FilesInfo.Count];
            for (int i = 0; i < FilesState.Length; ++i)
                FilesState[i] = new BitArray(torrentInfo.FilesInfo[i].BlocksCount, false);
        }

        /// <exception cref="ArgumentException">Wrong file index</exception>
        /// <exception cref="ArgumentException">Wrong length of BitArray</exception>
        public void SetFileState(int fileIndex, BitArray state)
        {
            if (fileIndex < 0 || fileIndex >= FilesState.Length)
                throw new ArgumentException("Wrong file index.");
            if (state.Length != FilesState[fileIndex].Length)
                throw new ArgumentException("Wrong length of state.");

            FilesState[fileIndex] = state;
        }

        /// <exception cref="ArgumentException">Wrong count of states</exception>
        /// <exception cref="ArgumentException">Wrong length of some state</exception>
        public void SetFilesState(IList<BitArray> filesState)
        {
            if (filesState.Count != FilesState.Length)
                throw new ArgumentException("Wrong count of states.");
            for (int i = 0; i < filesState.Count; ++i)
                if (filesState[i].Length != FilesState[i].Length)
                    throw new ArgumentException("Wrong length of one state.");

            for (int i = 0; i < filesState.Count; ++i)
                FilesState[i] = filesState[i];
        }

        /// <exception cref="ArgumentException">Wrong file index</exception>
        /// <exception cref="ArgumentException">Wrong block index</exception>
        public void SetBlockDone(int fileIndex, int blockIndex)
        {
            if (fileIndex < 0 || fileIndex >= FilesState.Length)
                throw new ArgumentException("Wrong file index.");
            if (blockIndex < 0 || blockIndex >= FilesState[fileIndex].Length)
                throw new ArgumentException("Wrong block index.");

            FilesState[fileIndex].Set(blockIndex, true);
        }

        public void SetAllBlocksDone()
        {
            foreach (BitArray doneMask in FilesState)
                doneMask.SetAll(true);
        }

        public Key GetKey()
        {
            return new Key(AccessCode, Address);
        }

        public class Key
        {
            public string AccessCode { get; private set; }
            public IPEndPoint Address { get; private set; }

            public Key(string accessCode, IPEndPoint address)
            {
                AccessCode = accessCode;
                Address = address;
            }

            public override int GetHashCode()
            {
                return AccessCode.GetHashCode() * 31 + Address.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                    return true;

                if (obj is Key another)
                    return AccessCode.Equals(another.AccessCode) && Address.Equals(another.Address);

                return false;
            }
        }
    }

}
