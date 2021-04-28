using System.IO;
using System.Reflection;
using System.Threading;

namespace System.TcpProcessor
{
    public class TcpServer
    {        
        public static void Start()
        {
            string PATH_WWW = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "www");
            if (!Directory.Exists(PATH_WWW)) Directory.CreateDirectory(PATH_WWW);
            //string PATH_LOGIN = Path.Combine(PATH_WWW, "login");
            //if (!Directory.Exists(PATH_LOGIN)) Directory.CreateDirectory(PATH_LOGIN);


            TcpHandler httpServer = new TcpHandler(12345, new TcpMessage(PATH_WWW));
            Thread thread = new Thread(new ThreadStart(httpServer.Listen));
            thread.Start();
        }
    }
}
