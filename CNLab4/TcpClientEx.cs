using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public static class TcpClientEx
    {
        /// <exception cref="TimeoutException">TIME   IS   OUT</exception>
        public static async Task<bool> TryConnectAsync(this TcpClient client, IPEndPoint address, int msTimeout)
        {
            Task timeoutTask = Task.Delay(msTimeout);
            Task connectionTask = client.ConnectAsync(address.Address, address.Port);

            Task doneTask = await Task.WhenAny(connectionTask, timeoutTask);

            if (doneTask == timeoutTask)
                return false;
            else
            {
                await connectionTask;
                return true;
            }
        }
    }
}
