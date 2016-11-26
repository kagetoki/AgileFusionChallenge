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
		private List<WebSocket> Clients = new List<WebSocket>();
		private object _sync = new object();
		public void ProcessRequest(HttpContext context)
		{
			if (context.IsWebSocketRequest)
			{
				context.AcceptWebSocketRequest(WebSocketRequestHandler);
                ProcessMonitor.Monitor.ResourceSnapshot +=  async (s, e) => { await SendToAll(e); };
                ProcessMonitor.Monitor.Stopped += async () => await SendToAll("Monitor stopped");
                ProcessMonitor.Monitor.Start();
			}
		}

		public bool IsReusable { get { return false; } }
        
		public async Task WebSocketRequestHandler(AspNetWebSocketContext webSocketContext)
		{
			WebSocket webSocket = webSocketContext.WebSocket;
			lock (_sync)
			{
				Clients.Add(webSocket);
			}
			
			const int maxMessageSize = 1024;
			var receivedDataBuffer = new ArraySegment<byte>(new byte[maxMessageSize]);
			var cancellationToken = new CancellationToken();
            
			while (webSocket.State == WebSocketState.Open)
			{
				WebSocketReceiveResult webSocketReceiveResult =
				  await webSocket.ReceiveAsync(receivedDataBuffer, cancellationToken);
                
				if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
					  string.Empty, cancellationToken);
					lock (_sync)
					{
						Clients.Remove(webSocket);
                        if(Clients.Count == 0)
                        {
                            ProcessMonitor.Monitor.Stop();
                        }
					}
				}
				else
				{
					var stats = ProcessMonitor.Monitor.GetSystemResourceConsumption();
					
					var newString = $"CPU Usage: {stats.CpuUsage}, RAM usage: {stats.RamUsage}";
					var sb = new StringBuilder(newString);
					foreach(var proc in stats.Processes)
					{
						sb.AppendLine(proc.ToString());
					}
					newString = sb.ToString();
					byte[] bytes = Encoding.UTF8.GetBytes(newString);
                    
					await webSocket.SendAsync(new ArraySegment<byte>(bytes),
					  WebSocketMessageType.Text, true, cancellationToken);
				}
			}
		}

		private async Task SendToAll(ResourceUsageEventArgs data)
		{
            var message = JsonConvert.SerializeObject(data);
            await SendToAll(message);
		}

        private async Task SendToAll(string message)
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                var client = Clients[i];
                var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                try
                {
                    await client.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (ObjectDisposedException)
                {
                    lock (_sync)
                    {
                        Clients.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}