//using log4net;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace System.TcpHandler.Http
{

    public class HttpServer
    {
        #region Fields

        private int Port;
        private TcpListener Listener;
        private HttpProcessor Processor;
        private bool IsActive = true;

        #endregion

        //private static readonly ILog log = LogManager.GetLogger(typeof(HttpServer));

        #region Public Methods
        public HttpServer(int port, List<Route> routes, ITcpMessage tcpMessage_ = null)
        {
            this.Port = port;
            this.Processor = new HttpProcessor(tcpMessage_);

            foreach (var route in routes)
            {
                this.Processor.AddRoute(route);
            }
        }

        public void Listen()
        {
            this.Listener = new TcpListener(IPAddress.Any, this.Port);
            this.Listener.Start();
            while (this.IsActive)
            {
                TcpClient s = this.Listener.AcceptTcpClient();
                Thread thread = new Thread(new ParameterizedThreadStart((object obj) =>
                {
                    TcpClient socket = (TcpClient)obj;
                    this.Processor.HandleClient(socket);
                }));
                thread.Start(s);
                Thread.Sleep(1);
            }
        }

        #endregion

    }
}



