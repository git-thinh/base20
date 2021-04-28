using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using _StoreToken = System.Collections.Concurrent.ConcurrentDictionary<string, int>;
using _StoreUser = System.Collections.Concurrent.ConcurrentDictionary<int, string>;

namespace F88.Mobility
{
    public class ServerTcp
    {
        const string URL_USER_GET_JSON_BY_ID = "http://localhost:3500/api/test/user/{0}";
        const string URL_USER_GET_TOKEN_BY_ID = "http://localhost:3500/local/user/token/{0}";
        const string URL_USER_GET_PROFILE_BY_TOKEN = "http://localhost:3500/local/user/profile/{0}";

        void ___process_message(NetworkStream stream, NOTI_TYPE type, string data) {
            if (string.IsNullOrEmpty(data)) return;

            byte[] buf;
            string[] a = data.Split('|');
            string msg, s;

            switch (type) {
                case NOTI_TYPE.TOKEN_VALID:
                    s = ___get_url(string.Format(URL_USER_GET_PROFILE_BY_TOKEN, a.Length > 1 ? a[1] : a[0]));
                    if (s.Length > 0)
                    {
                        //data = System.Web.HttpUtility.HtmlDecode(data);
                        buf = Convert.FromBase64String(data);
                        msg = Encoding.ASCII.GetString(buf);

                        stream.Write(buf, 0, buf.Length);
                        stream.Flush();
                    }
                    break;
                case NOTI_TYPE.TOKEN_RETURN_LOGIN_SUCCESS:
                    s = ___get_url(string.Format(URL_USER_GET_TOKEN_BY_ID, a.Length > 1 ? a[1] : a[0]));
                    if (s.Length > 0)
                    {
                        buf = Encoding.UTF8.GetBytes(s);
                        msg = Convert.ToBase64String(buf);
                        buf = Encoding.UTF8.GetBytes(msg);

                        stream.Write(buf, 0, buf.Length);
                        stream.Flush();
                    }
                    break;
            }
        }

        static string ___get_url(string url) {
            string s = string.Empty;
            
            try
            {
                using (WebClient client = new WebClient())
                {
                    var htmlData = client.DownloadData(url);
                    s = Encoding.UTF8.GetString(htmlData);
                }
            }
            catch (Exception ex) { 
            
            }

            return s;
        }

        #region
        
        TcpListener server = null;
        public ServerTcp(string ip, int port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
            StartListener();
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }

        public void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            try
            {
                byte[] bufs = new byte[Notify._BUFFER_SIZE];
                int i = stream.Read(bufs, 0, bufs.Length);
                if (i > 36)
                {
                    NOTI_TYPE type = (NOTI_TYPE)bufs[0];
                    string data = Encoding.UTF8.GetString(bufs, 1, i - 1);
                    if (data.Length > 36) data = data.Substring(36);
                    Console.WriteLine("{0}\t\t\t={1}", type, data);
                    ___process_message(stream, type, data);
                }
                client.Close();
            }
            catch (Exception e)
            {
                client.Close();
            }
        }
        public void HandleDeivceBak(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();

            string imei = String.Empty;

            string data = null;
            byte[] bufs = new byte[Notify._BUFFER_SIZE];
            int i;
            NOTI_TYPE type;

            try
            {
                while ((i = stream.Read(bufs, 0, bufs.Length)) != 0)
                {
                    if (i > 36)
                    {
                        type = (NOTI_TYPE)bufs[0];
                        data = Encoding.UTF8.GetString(bufs, 1, i - 1);
                        if (data.Length > 36) data = data.Substring(36);
                        Console.WriteLine("{0}\t\t\t={1}", type, data);
                    }


                    //string hex = BitConverter.ToString(bufs);
                    //data = Encoding.ASCII.GetString(bufs, 0, i);
                    //Console.WriteLine("{1}: Received: {0}", data, Thread.CurrentThread.ManagedThreadId);
                    //string str = "Hey Device!";
                    //Byte[] reply = System.Text.Encoding.ASCII.GetBytes(str);
                    //stream.Write(reply, 0, reply.Length);
                    //Console.WriteLine("{1}: Sent: {0}", str, Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }

        }
        
        #endregion
    }
}
