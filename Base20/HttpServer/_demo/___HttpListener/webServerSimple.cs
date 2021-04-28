using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace host
{
    class webServerSimple
    {

        private HttpListener listener;
        private bool firstRun = true;
        private const string prefixes = "http://127.0.0.1:8080/";
        private const string rootPagePhysicalPath = @"D:\Users\Bob\Documents\computers\Web sites\Ceres\root.html";
        private const string siteStylesheetPhysicalPath = @"D:\Users\Bob\Documents\computers\Web sites\Ceres\site.css";
        private const string favIconPhysicalPath = @"D:\Users\Bob\Documents\computers\Web sites\Ceres\favicon.ico";

        public void Start(int port)
        {
            ServicePointManager.DefaultConnectionLimit = 500;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.MaxServicePoints = 500;


            listener = new HttpListener();

            if (firstRun)
            {
                //listener.Prefixes.Add(prefixes);
                listener.Prefixes.Add(String.Format(@"http://+:{0}/", port));
            }

            firstRun = false;

            listener.Start();
            listen();
        }

        private void listen()
        {
            IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
            result.AsyncWaitHandle.WaitOne();
        }

        public static void ListenerCallback22(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }

        private void ListenerCallback(IAsyncResult result)
        {
            HttpListenerContext context = listener.EndGetContext(result);

            string path = "";
            HttpListenerRequest request = context.Request;
            switch (request.RawUrl)
            {
                case "/":
                    path = rootPagePhysicalPath;
                    break;
                case "/root.html":
                    path = rootPagePhysicalPath;
                    break;
                case "/site.css":
                    path = siteStylesheetPhysicalPath;
                    break;
                case "/favicon.ico":
                    path = favIconPhysicalPath;
                    break;
            }

            HttpListenerResponse response = context.Response;
            StreamReader streamReader = new StreamReader(path);
            string responseString = streamReader.ReadToEnd();
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;

            output.Write(buffer, 0, buffer.Length);

            output.Close();

            listen();
        }

        public void Stop()
        {
            listener.Close(); // calling this method causes the above exception
        }
    }
}
