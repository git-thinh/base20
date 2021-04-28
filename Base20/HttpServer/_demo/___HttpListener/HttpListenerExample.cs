using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace host
{
    class HttpListenerExample
    {
        static void Main1111(string[] args)
        {
            var server = new LocalHttpListener();
            var task = Task.Factory.StartNew(() => server.Start());

            var webRequest = CreateWebRequest();

            using (var webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                var info = LocalHttpListener.Read(webResponse);
                Console.WriteLine("Client received: " + info.ToString());
            }

            server.Stop();
        }

        private static WebRequest CreateWebRequest()
        {
            var webRequest = WebRequest.Create(LocalHttpListener.UriAddress);
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml";
            webRequest.Credentials = CredentialCache.DefaultCredentials;
            string xmlBody = ConvertToXmlString("Some text for the body");
            SetRequestBody(webRequest, xmlBody);
            return webRequest;
        }

        private static void SetRequestBody(WebRequest webRequest, string body)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(body);
            webRequest.ContentLength = buffer.Length;
            using (Stream requestStream = webRequest.GetRequestStream())
                requestStream.Write(buffer, 0, buffer.Length);
        }

        private static string ConvertToXmlString(object body)
        {
            string xmlBody;
            var xmlSerializer = new XmlSerializer(body.GetType());
            using (StringWriter writer = new StringWriter())
            {
                xmlSerializer.Serialize(writer, body);
                xmlBody = writer.ToString();
            }
            return xmlBody;
        }
    }
}