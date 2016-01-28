using Bonsai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using WebSocket4Net;
using System.Diagnostics;

// TODO: replace this with the sink input type.
using TSource = System.String;

namespace BonsaiWeb
{
    public class WebSocketSink : Sink<TSource>
    {
        public string url { get; set; } = "ws://mt.champalimaud.pt:8080";

        public override IObservable<TSource> Process(IObservable<TSource> source)
        {
            var connection = WebSocketManager.ReserveConnection(url);
            WebSocket ws = connection.WebSocket;

            source.Subscribe((str) =>
            {

                if (ws.State == WebSocketState.Open)
                {
                    Debug.WriteLine("Sending:" + str);
                    ws.Send(str);
                } else
                {
                    Debug.WriteLine("Socket is not open...");
                }
            });

            return source;
        }
    }
}
