using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4_Client
{
    class LogEventArgs : EventArgs
    {
        public string[] Lines;
    }

    static class General
    {
        public static readonly IPEndPoint ServerAddress = new IPEndPoint(IPAddress.Loopback, 59399);
        public static readonly IPAddress PeerIP;
        private static int _peerPort = 0;
        public static int PeerPort
        {
            get => _peerPort;
            set
            {
                _peerPort = value;
                PeerAddress = new IPEndPoint(PeerIP, _peerPort);
            }
        }
        public static IPEndPoint PeerAddress { get; private set; }

        public static event EventHandler<LogEventArgs> OnNewLog;

        static General()
        {
            //try
            //{
            //    string ipString = new WebClient().DownloadString("https://ipinfo.io/ip/").Replace("\n", "");
            //    PeerIP = IPAddress.Parse(ipString);
            //}
            //catch
            //{
            //    PeerIP = IPAddress.Loopback;
            //}

            PeerIP = IPAddress.Loopback;
            PeerAddress = new IPEndPoint(PeerIP, PeerPort);
        }

        public static void Log(string line)
        {
            line = $"{DateTime.Now.ToString("HH:mm:ss")}: {line}";
            OnNewLog?.Invoke(null, new LogEventArgs { Lines = new string[] { line } });
        }

        public static void Log(params string[] lines)
        {
            string firstLinePrefix = $"{DateTime.Now.ToString("HH:mm:ss")}: ";
            if (lines.Length > 0)
                lines[0] = firstLinePrefix + lines[0];
            for (int i = 1; i < lines.Length; ++i)
            {
                StringBuilder builder = new StringBuilder();
                for (int j = 0; j < firstLinePrefix.Length; ++j)
                    builder.Append(' ');
                builder.Append(lines[i]);
                lines[i] = builder.ToString();
            }
            OnNewLog?.Invoke(null, new LogEventArgs { Lines = lines });
        }

        public static string GetSizeStrRepr(long size)
        {
            if (size < 1024)
                return $"{size} B";
            else if (size < 1048576)
            {
                double kbSize = size / 1024d;
                return $"{kbSize.ToString("F1")} KB";
            }
            else if (size < 1_073_741_824)
            {
                double mbSize = size / 1024d / 1024d;
                return $"{mbSize.ToString("F1")} MB";
            }
            else
            {
                double mbSize = size / 1024d / 1024d / 1024d;
                return $"{mbSize.ToString("F1")} GB";
            }
        }
    }
}
