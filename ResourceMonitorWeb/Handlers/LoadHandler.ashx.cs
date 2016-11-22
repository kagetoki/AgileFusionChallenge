using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using ProcessMonitor;
using Newtonsoft.Json;
using System.Text;

namespace ResourceMonitorWeb.Handlers
{
    /// <summary>
    /// Summary description for LoadHandler
    /// </summary>
    public class LoadHandler : IHttpHandler
    {
        private readonly IList<WebSocket> Clients = new List<WebSocket>();
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
		public LoadHandler()
		{
			ProcessMonitor.Monitor.HighLoadHappend += (s, e) => SendMessage(e);
		}
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
        }

		private void SendMessage(ResourceUsageEventArgs model)
		{
			var message = JsonConvert.SerializeObject(model);
			var bytes = Encoding.UTF8.GetBytes(message);
			var arraySegment = new ArraySegment<byte>(bytes);
			for(int i = 0; i < Clients.Count; i++)
			{
				var client = Clients[i];
				try
				{
					client.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
				}
				catch (ObjectDisposedException)
				{
					_locker.EnterWriteLock();
					try
					{
						Clients.Remove(client);
						i--;
					}
					finally
					{
						_locker.ExitWriteLock();
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