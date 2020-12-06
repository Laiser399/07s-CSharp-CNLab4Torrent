using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Peer.Responses
{
    public class DoneMasksResponse : BasePeerResponse
    {
        public IList<BitArray> DoneMasks;
    }
}
