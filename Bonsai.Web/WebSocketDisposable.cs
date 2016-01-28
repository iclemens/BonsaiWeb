using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace BonsaiWeb
{
    public sealed class WebSocketDisposable : ICancelable, IDisposable
    {
        IDisposable resource;

        public WebSocketDisposable(WebSocket ws, IDisposable disposable)
        {
            if(ws == null)
            {
                throw new ArgumentNullException("websocket");
            }

            if(disposable == null)
            {
                throw new ArgumentNullException("disposable");
            }

            WebSocket = ws;
            resource = disposable;
        }

        public WebSocket WebSocket { get; private set; }

        public bool IsDisposed
        {
            get { return resource == null; }
        }

        public void Dispose()
        {
            var disposable = Interlocked.Exchange<IDisposable>(ref resource, null);
            if(disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
