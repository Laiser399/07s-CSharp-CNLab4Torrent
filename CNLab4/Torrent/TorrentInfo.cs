using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    [JsonConverter(typeof(TorrentInfoConverter))]
    public class TorrentInfo
    {
        public string Name { get; private set; }
        private TorrentFileInfo[] _filesInfo;
        public IReadOnlyList<TorrentFileInfo> FilesInfo => Array.AsReadOnly(_filesInfo);
        public string AccessCode { get; private set; }
        public long FullSize { get; private set; }

        public TorrentInfo(string name, IEnumerable<TorrentFileInfo> filesInfo, string accessCode)
        {
            Name = name;
            _filesInfo = filesInfo.ToArray();
            AccessCode = accessCode;
            FullSize = CalcFullSize();
        }

        public TorrentInfo(string name, TorrentFileInfo fileInfo, string accessCode)
        {
            Name = name;
            _filesInfo = new TorrentFileInfo[] { fileInfo };
            AccessCode = accessCode;
            FullSize = CalcFullSize();
        }

        private long CalcFullSize()
        {
            long size = 0;
            foreach (var fInfo in _filesInfo)
                size += fInfo.FileLength;
            return size;
        }
    }
}
