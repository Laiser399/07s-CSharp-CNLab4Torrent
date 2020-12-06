using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CNLab4.Messages.Server.Requests
{
    public class GetPeers : BaseServerRequest
    {
        public string AccessCode;
    }
}
