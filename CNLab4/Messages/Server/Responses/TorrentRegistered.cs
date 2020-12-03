using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Server.Responses
{
    public class TorrentRegistered : BaseServerResponse
    {
        public string AccessCode;
        public IReadOnlyList<int> BlocksSizes;
    }
}
