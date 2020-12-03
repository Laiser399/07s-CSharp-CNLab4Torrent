using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Server.Responses
{
    public class PeersInfoResponse : BaseServerResponse
    {
        public IList<Info> Infos;
        
        public class Info
        {
            public IPEndPoint Address;
            public IList<BitArray> FilesMasks;
        }
    }
}
