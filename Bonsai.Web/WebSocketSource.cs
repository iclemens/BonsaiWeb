using Bonsai;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Diagnostics;
using WebSocket4Net;
using SuperSocket.ClientEngine;

using TSource = System.String;


namespace BonsaiWeb
{
    public class WebSocketSource : Source<TSource>
    {
        public string url { get; set; } = "ws://mt.champalimaud.pt:8080";

        public override IObservable<TSource> Generate()
        {
            return Observable.Create<string>((IObserver<string> observer) =>
                {
                    var connection = WebSocketManager.ReserveConnection(url);
                    WebSocket ws = connection.WebSocket;

                    var messageReceivedHandler = new EventHandler<MessageReceivedEventArgs>((sender, e) => {
                        observer.OnNext(e.Message);
                        });

                    var openedHandler = new EventHandler((sender, e) => { Debug.WriteLine("Opened"); });
                    var errorHandler = new EventHandler<ErrorEventArgs>((sender, e) => observer.OnError(e.Exception));
                    var closedHandler = new EventHandler((sender, e) => observer.OnCompleted());

                    ws.Opened += openedHandler;
                    ws.MessageReceived += messageReceivedHandler;
                    ws.Error += errorHandler;
                    ws.Closed += closedHandler;

                    return Disposable.Create(() => {
                        ws.Opened -= openedHandler;
                        ws.MessageReceived -= messageReceivedHandler;
                        ws.Error -= errorHandler;
                        ws.Closed -= closedHandler;

                        connection.Dispose();
                        });
               });
        }
    }
}
