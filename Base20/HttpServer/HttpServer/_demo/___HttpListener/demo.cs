using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace host.HTTP.HttpListener
{
    class demo
    {
        void init() {

            //Task.Factory.StartNew(() =>
            //{
            //List<Uri> ls_uri = new List<Uri>() { 
            //    new Uri("http://127.0.0.1:" + main.config.site_port.ToString() + "/"),
            //    new Uri("http://localhost:" + main.config.site_port.ToString() + "/")                    
            //};
            //if (main.config.site_ip != "127.0.0.1") ls_uri.Add(new Uri(main.page_Site + "/"));

            //var cf = new host.Nancy.Hosting.Self.HostConfiguration();
            //cf.RewriteLocalhost = false;
            //var nancyHost = new host.Nancy.Hosting.Self.NancyHost(cf, ls_uri.ToArray());
            //nancyHost.Start();

            //main.show_notification(main.page_Site, 3000);
            //});
            //------------------------------------------------------------------------------------------

            //List<string> ls_uri = new List<string>() { 
            //    "http://127.0.0.1:" + main.config.site_port.ToString() + "/",
            //    "http://localhost:" + main.config.site_port.ToString() + "/"                    
            //};
            //if (main.config.site_ip != "127.0.0.1") ls_uri.Add(main.page_Site + "/");

            //WebServer ws = new WebServer(SendResponse, ls_uri.ToArray());
            //ws.Run();
            ////ws.Stop();
            //main.show_notification(main.page_Site, 3000);



            //HttpServer ws = new HttpServer(100);
            //ws.Start(config.site_port);
            //ws.ProcessRequest += ws_ProcessRequest;

        }

        private static void ws_ProcessRequest(HttpListenerContext ctx)
        {
            try
            {
                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                    ctx.Response.AddHeader("Access-Control-Max-Age", "1728000");
                }
                ctx.Response.AppendHeader("Access-Control-Allow-Origin", "*");

                string rstr = SendResponse(ctx.Request);
                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                ctx.Response.ContentLength64 = buf.Length;
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            }
            catch { } // suppress any exceptions
            finally
            {
                // always close the stream
                ctx.Response.OutputStream.Close();
            }
        }//end main function

        public static string SendResponse(HttpListenerRequest request)
        {
            return string.Format("<HTML><BODY>My web page.<br>{0}</BODY></HTML>", DateTime.Now);
        }

    }
}
