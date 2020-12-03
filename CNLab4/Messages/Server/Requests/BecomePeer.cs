using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Server.Requests
{
    public class BecomePeer : BaseServerRequest
    {
        public string AccessCode;
        public IPEndPoint Address;
        public IList<Block> AvailableBlocks;
    }
}
