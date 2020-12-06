using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Peer.Requests
{
    public class GetBlock : BasePeerRequest
    {
        public string AccessCode;
        public int FileIndex;
        public int BlockIndex;
    }
}
