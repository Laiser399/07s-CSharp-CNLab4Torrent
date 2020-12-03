using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Peer.Requests
{
    public class GetBlocks : BasePeerRequest
    {
        public string AccessCode;
        public IList<FileBlocks> BlocksNeed;
    }
}
