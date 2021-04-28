using host.Http;
using host.Http.Sessions;
using model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace host
{
    public class webResponse
    {
        public static void write_page(IHttpRequest request, IHttpResponse response, IHttpSession session, string page_content)
        {
            StreamWriter writer = new StreamWriter(response.Body);
            writer.WriteLine(page_content);
            writer.Flush();
        }




        /*

        public static void write_Page(HttpListenerResponse response, m_cache item)
        {
            try
            {
                response.KeepAlive = true;
                response.SendChunked = true;
                if (item.Cache)
                {
                    response.AddHeader("Last-Modified", "Sun, 14 Nov 2010 21:15:21 GMT");
                    response.ProtocolVersion = new Version("1.1");
                    response.AddHeader("Cache-Control", "max-age=86400");
                    response.AddHeader("ETag", string.Format("\"{0}\"", Guid.NewGuid().ToString()));
                }
                //response.AddHeader("Accept-Ranges", "bytes");
                response.ContentType = item.MimeType;

                response.ContentLength64 = item.Content.Length;
                response.OutputStream.Write(item.Content, 0, item.Content.Length);
            }
            catch { } // suppress any exceptions
            finally
            {
                // always close the stream
                response.OutputStream.Close();
            }
        }

        public static void write_PageRedirect(HttpListenerResponse response, string uri, Dictionary<string, string> pageCookie = null)
        {
            StringBuilder bi = new StringBuilder() { };
            bi.Append("<script>" + Environment.NewLine);
            if (pageCookie != null && pageCookie.Count > 0)
            {
                bi.Append(@"
                            function apiCooSet(c_name, value) {
                                var exdays = 300;
                                var exdate = new Date();
                                exdate.setDate(exdate.getDate() + exdays);
                                var c_value = escape(value) + ((exdays == null) ? '' : '; expires=' + exdate.toUTCString());
                                document.cookie = c_name + '=' + c_value;
                            }
                            ");

                foreach (var ki in pageCookie)
                    bi.Append("apiCooSet('" + ki.Key + "','" + ki.Value + "');" + Environment.NewLine);

            }
            bi.Append("location.href = '" + uri + "';" + Environment.NewLine);
            bi.Append("</script>" + Environment.NewLine);
            write_PageBody(response, bi.ToString());
        }

        public static void write_PageBody(HttpListenerResponse response, string body, string mime_type = "text/html", bool cache = true)
        {
            try
            {
                string page = string.Format(m_page, body);

                response.KeepAlive = true;
                response.SendChunked = true;
                if (cache)
                {
                    response.AddHeader("Last-Modified", "Sun, 14 Nov 2010 21:15:21 GMT");
                    response.ProtocolVersion = new Version("1.1");
                    response.AddHeader("Cache-Control", "max-age=86400");
                    response.AddHeader("ETag", string.Format("\"{0}\"", Guid.NewGuid().ToString()));
                }
                //response.AddHeader("Accept-Ranges", "bytes");
                response.ContentType = mime_type;

                byte[] buf = Encoding.UTF8.GetBytes(body);
                response.ContentLength64 = buf.Length;
                response.OutputStream.Write(buf, 0, buf.Length);
            }
            catch { } // suppress any exceptions
            finally
            {
                // always close the stream
                response.OutputStream.Close();
            }
        }

        public static void write_Print()
        {

        }

        */
        private static string m_page =
@"<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title></title>
</head>
<body>
{0}
</body>
</html>";
    }//end class
}
