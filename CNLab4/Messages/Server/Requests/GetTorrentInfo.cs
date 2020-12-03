using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Server.Requests
{
    public class GetTorrentInfo : BaseServerRequest
    {
        public string AccessCode;
    }
}
