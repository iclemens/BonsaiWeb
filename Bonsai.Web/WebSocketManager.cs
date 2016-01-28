using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using WebSocket4Net;

namespace BonsaiWeb
{
    public static class WebSocketManager
    {
        static readonly Dictionary<string, Tuple<WebSocket, RefCountDisposable>> openConnections =
            new Dictionary<string, Tuple<WebSocket, RefCountDisposable>>();

        static readonly object openConnectionsLock = new object();

        public static WebSocketDisposable ReserveConnection(string uri)
        {
            Tuple<WebSocket, RefCountDisposable> connection;
            lock(openConnectionsLock)
            {
                if(!openConnections.TryGetValue(uri, out connection))
                {
                    Debug.WriteLine("Creating new websocket");

                    WebSocket ws = new WebSocket(uri);
                    // Set callbacks?
                    ws.Open();

                    var dispose = Disposable.Create(() =>
                    {
                        Debug.WriteLine("Disposing of websocket");
                        ws.Close();
                        openConnections.Remove(uri);
                    });

                    RefCountDisposable refCount = new RefCountDisposable(dispose);
                    connection = Tuple.Create(ws, refCount);
                    openConnections.Add(uri, connection);

                    return new WebSocketDisposable(connection.Item1, connection.Item2.GetDisposable());
                }                
            }

            Debug.WriteLine("Returning old websocket");
            return new WebSocketDisposable(connection.Item1, connection.Item2.GetDisposable());
        }
    }
}
