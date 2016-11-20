using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace ResourceMonitorWeb.Handlers
{
    /// <summary>
    /// Summary description for LoadHandler
    /// </summary>
    public class LoadHandler : IHttpHandler
    {
        private readonly IList<WebSocket> Clients = new List<WebSocket>();
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(WebSocketRequest);
            }
        }

        private async Task WebSocketRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket;
            _locker.EnterWriteLock();
            try
            {
                Clients.Add(socket);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
            while (true)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);

                // Ожидаем данные от него
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);


                //Передаём сообщение всем клиентам
                for (int i = 0; i < Clients.Count; i++)
                {

                    WebSocket client = Clients[i];

                    try
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }

                    catch (ObjectDisposedException)
                    {
                        try
                        {
                            Clients.Remove(socket);
                            i--;
                        }
                        finally
                        {
                            _locker.ExitWriteLock();
                        }
                    }
                }

            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}