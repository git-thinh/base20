//using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace System.TcpProcessor
{
    public class oUserLogin
    {
        public string username { set; get; }
        public string password { set; get; }
    }

    public enum TCP_MSG_TYPE
    {
        HTTP_GET = 71,
        HTTP_POS = 80
    }

    public interface ITcpMessage
    {
        string PATH_WWW { get; }
        void Process(Stream stream, int type);
    }

    public class TcpMessage : ITcpMessage
    {
        public string PATH_WWW { get; }
        public TcpMessage(string path_www)
        {
            this.PATH_WWW = path_www;
        }

        //const int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB
        const int MAX_BUFFER_SIZE_GET = 3 * 1024; // 3KB
        const int MAX_BUFFER_SIZE_POST = 500 * 1024; // 500KB

        void ___print_log(string text)
        {
            Console.WriteLine(text);
        }

        public void Process(Stream stream, int type)
        {
            int size;
            byte[] buf;
            string[] a;
            string request, url;
            bool go_login = false;

            TCP_MSG_TYPE msgType = (TCP_MSG_TYPE)type;
            switch (msgType)
            {
                case TCP_MSG_TYPE.HTTP_GET:
                    #region
                    buf = new byte[MAX_BUFFER_SIZE_GET];
                    size = stream.Read(buf, 0, MAX_BUFFER_SIZE_GET);
                    if (size > 2)
                    {
                        request = Encoding.UTF8.GetString(buf, 0, size).Trim();
                        a = request.Split(' ');
                        if (a.Length > 2)
                        {
                            url = a[1];
                            ___print_log(string.Format("GET: {0}", url));
                            switch (url)
                            {
                                case "/favicon.ico":
                                    break;
                                default:
                                    go_login = token___valid_go_login(stream, "GET", url, request);
                                    if (go_login == false) response___get(stream, url, request);
                                    break;
                            }
                        }
                    }
                    #endregion
                    break;
                case TCP_MSG_TYPE.HTTP_POS:
                    #region
                    buf = new byte[MAX_BUFFER_SIZE_POST];
                    size = stream.Read(buf, 0, MAX_BUFFER_SIZE_POST);
                    if (size > 2)
                    {
                        request = Encoding.UTF8.GetString(buf, 0, size).Trim();
                        a = request.Split(' ');
                        if (a.Length > 2)
                        {
                            url = a[1];
                            ___print_log(string.Format("POST: {0}", url));
                            go_login = token___valid_go_login(stream, "POST", url, request);
                            if (go_login == false) response___post(stream, url, request);
                        }
                    }
                    #endregion
                    break;
                default:
                    break;
            }
        }

        void response___get(Stream stream, string url, string request)
        {
            string path = url.Split('?')[0];
            switch (path)
            {
                case "/login":
                    response___page_login(stream);
                    break;
                default:
                    if (url.StartsWith("/_"))
                    {
                        response___write_static_file(stream, PATH_WWW, path);
                    }
                    else
                    {
                        response___write_time(stream);
                    }
                    break;
            }
        }

        void response___post(Stream stream, string url, string request)
        {
            string[] a;
            string path = url.Split('?')[0], data = string.Empty;
            bool valid_data = false;

            a = request.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);
            if (a.Length > 1)
            {
                data = request.Substring(a[0].Length, request.Length - a[0].Length).Trim();
                if (data.Length > 2
                    && ((data[0] == '{' || data[0] == '['))
                    && ((data[0] == '{' || data[0] == '[')))
                {
                    valid_data = true;
                }
            }

            if (valid_data)
            {
                switch (path)
                {
                    case "/login/check":
                        #region
                        try
                        {
                            var user = JsonConvert.DeserializeObject<oUserLogin>(data);
                            string token = string.Empty;

                            //if (_USER__TOKENS.ContainsKey(user.username) == false)
                            //{
                            //    token = Guid.NewGuid().ToString();
                            //    _USER__TOKENS.TryAdd(user.username, token);
                            //}
                            //else
                            //{
                            //    _USER__TOKENS.TryGetValue(user.username, out token);
                            //}

                            token = Guid.NewGuid().ToString();
                            _TOKENS_USER.TryAdd(token, user.username);

                            if (token.Length > 0)
                            {
                                response___write_json(stream, new { ok = true, message = token });
                            }
                            else
                            {
                                response___write_json(stream, new { ok = false, message = "Error: cannot create a token for user logined success" });
                            }
                        }
                        catch (Exception ex)
                        {
                            response___write_json(stream, new { ok = false, message = "Data must be: { username:'...', password: '...' }. Error: " + ex.Message });
                        }
                        #endregion
                        break;
                    case "/api/test":
                        response___write_json(stream, new { ok = true, data = new { time = DateTime.Now } });
                        break;
                    default:
                        response___write_time(stream);
                        break;
                }
            }
            else
            {
                response___write_json(stream, new { ok = false, message = "The format data of POST must be type JSON" });
            }
        }

        #region [ TOKEN - LOGIN ]

        static ConcurrentDictionary<string, string> _TOKENS_USER = new ConcurrentDictionary<string, string>() { };

        bool token___valid_go_login(Stream stream, string method, string url, string request)
        {
            bool go_login = false;

            switch (method)
            {
                case "GET":
                    if (url == "/" || url.Contains("/api/"))
                    {
                        go_login = true;
                        response___redirect_page_login(stream, url);
                    }
                    break;
                case "POST":
                    if (!url.Contains("/login/check"))
                    {
                        string token = string.Empty;

                        if (request.Contains("\r\ntoken: "))
                        {

                            string[] a = request.Split(new string[] { "\r\ntoken: " }, StringSplitOptions.None);
                            if (a.Length > 1) token = a[1];
                            if (token.Length > 36) token = token.Substring(0, 36);
                        }

                        if (token.Length == 36 && _TOKENS_USER.ContainsKey(token))
                        {
                            ;
                        }
                        else
                        {
                            go_login = true;
                            response___write_json(stream, new { ok = false, message = "Token invalid" });
                        }
                    }
                    break;
            }
            return go_login;
        }

        void response___redirect_page_login(Stream stream, string url)
        {
            response___write(stream, Encoding.ASCII.GetBytes(string.Format(@"<meta http-equiv=""refresh"" content=""0;url=/login?url={0}"">", HttpUtility.UrlEncode(url))));
        }

        void response___page_login(Stream stream)
        {
            string html = "";
            string file = Path.Combine(PATH_WWW, "login\\index.html");

            if (File.Exists(file))
            {
                html = File.ReadAllText(file);
            }

            response___write(stream, Encoding.UTF8.GetBytes(html));
        }

        #endregion

        #region [ HTTP RESPONSE ]

        static void response___write_static_file(Stream stream, string root_path, string path_file)
        {
            string path = path_file.Replace('/', '\\');
            if (path.Length > 0 && path[0] == '\\') path = path.Substring(1);

            string file = Path.Combine(root_path, path);

            if (File.Exists(file))
            {
                //string html = "";
                //html = File.ReadAllText(file);
                //response___write(stream, Encoding.UTF8.GetBytes(html));

                string file_extension = path.Substring(path.Length - 4, 4);
                string contentType = QuickMimeTypeMapper.GetMimeType(file_extension);
                byte[] buf = File.ReadAllBytes(file);
                response___write(stream, buf, contentType);
            }
        }

        static void streamWrite(Stream stream, string text)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch { }
        }

        static void response___write_json(Stream stream, object data)
        {
            string json = "{}";
            if (data != null) json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            response___write(stream, Encoding.UTF8.GetBytes(json), "application/json");
        }

        static void response___write_text(Stream stream, string text) { response___write(stream, Encoding.UTF8.GetBytes(text), "text/plain"); }

        static void response___write_html(Stream stream, string text) { response___write(stream, Encoding.UTF8.GetBytes(text)); }

        static void response___write(Stream stream, byte[] Content, string contentType = "text/html")
        {
            string ReasonPhrase = "";
            try
            {
                if (Content == null)
                    Content = new byte[] { };

                var Headers = new Dictionary<string, string>() {
                    { "Content-Type", contentType },
                    { "Content-Length", Content.Length.ToString() }
                };

                streamWrite(stream, string.Format("HTTP/1.0 {0} {1}\r\n", 200, ReasonPhrase));
                streamWrite(stream, string.Join("\r\n", Headers.Select(x => string.Format("{0}: {1}", x.Key, x.Value)).ToArray()));
                streamWrite(stream, "\r\n\r\n");

                stream.Write(Content, 0, Content.Length);
                stream.Flush();
            }
            catch
            {
            }
        }

        static void response___write_time(Stream stream)
        {
            byte[] buf = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
            response___write(stream, buf);
        }

        #endregion

    }

}
