using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CNLab4_Server
{
    class Server
    {
        private TcpListener _listener;
        private bool _isStarted = false;


        public Server()
        {
            _listener = new TcpListener(IPAddress.Any, 59399);
        }

        public async void Start()
        {
            if (_isStarted)
                return;
            _isStarted = true;

            _listener.Start();

            while (_isStarted)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                OnClientAcceptedAsync(client);
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;
            _isStarted = false;
        }

        private async void OnClientAcceptedAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();

                JObject obj = await stream.ReadJObjectAsync();
                
                switch (obj.Value<string>("type"))
                {
                    case "register_torrent":
                        {
                            break;
                        }
                }
            }
        }
    }
}
