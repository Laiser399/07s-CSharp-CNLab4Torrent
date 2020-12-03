using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4.Messages.Server.Requests
{
    public class RegisterTorrent : BaseServerRequest
    {
        public IPEndPoint SenderAddress;
        public string TorrentName;
        public IList<FileInfo> FilesInfo;
        
        public class FileInfo
        {
            public string RelativePath;
            public long Length;
        }
    }
}
