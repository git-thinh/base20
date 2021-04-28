using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace host
{
    public class HTTPServer22
    {

        private HttpListener listener = new HttpListener();

        public void Start()
        {
            ServicePointManager.DefaultConnectionLimit = 500;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.MaxServicePoints = 500;

            listener.Prefixes.Add(String.Format(@"http://+:{0}/", 8888));
            listener.Start();

            for (int k = 1; k < (System.Environment.ProcessorCount * 2); k++)
            {
                System.Threading.Thread NewThread = new System.Threading.Thread(ListenerThread);
                NewThread.Priority = ThreadPriority.Normal;
                NewThread.IsBackground = true;
                NewThread.Start();
            }
        }



        private void ListenerThread()
        {
            while (true)
            {
                IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
                result.AsyncWaitHandle.WaitOne();
            }
        }


        private void ListenerCallback(IAsyncResult result)
        {
            HttpListenerContext context = listener.EndGetContext(result);

            //string path = "";
            //HttpListenerRequest request = context.Request;
            //switch (request.RawUrl)
            //{
            //    case "/":
            //        path = rootPagePhysicalPath;
            //        break;
            //    case "/root.html":
            //        path = rootPagePhysicalPath;
            //        break;
            //    case "/site.css":
            //        path = siteStylesheetPhysicalPath;
            //        break;
            //    case "/favicon.ico":
            //        path = favIconPhysicalPath;
            //        break;
            //}

            HttpListenerResponse response = context.Response;
            //StreamReader streamReader = new StreamReader(path);
            //string responseString = streamReader.ReadToEnd();

            string responseString = "OK";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;

            output.Write(buffer, 0, buffer.Length);

            output.Close();

        }

        //Private Shared Sub ListenerCallback(ByVal StateObject As IAsyncResult)

        //    Dim Listener As HttpListener = DirectCast(StateObject.AsyncState, HttpListener)

        //    Dim Context As HttpListenerContext = Listener.EndGetContext(StateObject)
        //    Dim Request As HttpListenerRequest = Context.Request

        //    Dim Response As HttpListenerResponse = Context.Response

        //    Dim ResponseString As String = "OK"

        //    Dim Buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(ResponseString)
        //    Response.ContentLength64 = Buffer.Length
        //    Dim OutputStream As System.IO.Stream = Response.OutputStream
        //    OutputStream.Write(Buffer, 0, Buffer.Length)

        //    OutputStream.Close()
        //    OutputStream.Dispose()

        //End Sub

    }
}
