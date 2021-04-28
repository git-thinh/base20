using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using host.Http.Exceptions;
using host.Http.Sessions;
using System.Text;
using System.Linq;
using Google.Apis.Drive.v2.Data.Models;
using Google.Apis.Authentication;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Drive.v2;

namespace host.Http.HttpModules
{
    /// <summary>
    /// The purpose of this module is to serve files.
    /// </summary>
    public class FileModule : HttpModule
    {
        private readonly string _baseUri;
        private readonly string _basePath;
        private readonly bool _useLastModifiedHeader;
        private readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();
        private static readonly string[] DefaultForbiddenChars = new string[] { "\\", "..", ":" };
        private string[] _forbiddenChars;
        private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class.
        /// </summary>
        /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
        /// <param name="basePath">Path on hard drive where we should start looking for files</param>
        /// <param name="useLastModifiedHeader">If true a Last-Modifed header will be sent upon requests urging web browser to cache files</param>
        public FileModule(string baseUri, string basePath, bool useLastModifiedHeader)
        {
            Check.Require(baseUri, "baseUri");
            Check.Require(basePath, "basePath");

            _useLastModifiedHeader = useLastModifiedHeader;
            _baseUri = baseUri;
            _basePath = basePath;
            if (!_basePath.EndsWith(PathSeparator))
                _basePath += PathSeparator;
            ForbiddenChars = DefaultForbiddenChars;

            if (_mimeTypes.Count == 0) AddDefaultMimeTypes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class.
        /// </summary>
        /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
        /// <param name="basePath">Path on hard drive where we should start looking for files</param>
        public FileModule(string baseUri, string basePath)
            : this(baseUri, basePath, false)
        { }

        /// <summary>
        /// List with all mime-type that are allowed. 
        /// </summary>
        /// <remarks>All other mime types will result in a Forbidden http status code.</remarks>
        public IDictionary<string, string> MimeTypes
        {
            get { return _mimeTypes; }
        }

        /// <summary>
        /// characters that may not  exist in a path.
        /// </summary>
        /// <example>
        /// fileMod.ForbiddenChars = new string[]{ "\\", "..", ":" };
        /// </example>
        public string[] ForbiddenChars
        {
            get { return _forbiddenChars; }
            set { _forbiddenChars = value; }
        }


        /// <summary>
        /// Mimtypes that this class can handle per default
        /// </summary>
        public void AddDefaultMimeTypes()
        {
            MimeTypes.Add("default", "application/octet-stream");
            MimeTypes.Add("txt", "text/plain");
            MimeTypes.Add("md", "text/plain");
            MimeTypes.Add("json", "application/json");
            MimeTypes.Add("html", "text/html");
            MimeTypes.Add("htm", "text/html");
            MimeTypes.Add("jpg", "image/jpg");
            MimeTypes.Add("jpeg", "image/jpg");
            MimeTypes.Add("bmp", "image/bmp");
            MimeTypes.Add("gif", "image/gif");
            MimeTypes.Add("png", "image/png");

            MimeTypes.Add("svg", "image/svg+xml");
            MimeTypes.Add("woff2", "font/woff2");
            MimeTypes.Add("woff", "application/font-woff");
            MimeTypes.Add("ttf", "font/truetype");
            MimeTypes.Add("eot", "application/vnd.ms-fontobject");
            MimeTypes.Add("otf", "font/opentype");

            MimeTypes.Add("ico", "image/vnd.microsoft.icon");
            MimeTypes.Add("css", "text/css");
            MimeTypes.Add("gzip", "application/x-gzip");
            MimeTypes.Add("zip", "multipart/x-zip");
            MimeTypes.Add("tar", "application/x-tar");
            MimeTypes.Add("pdf", "application/pdf");
            MimeTypes.Add("rtf", "application/rtf");
            MimeTypes.Add("xls", "application/vnd.ms-excel");
            MimeTypes.Add("ppt", "application/vnd.ms-powerpoint");
            MimeTypes.Add("doc", "application/application/msword");
            MimeTypes.Add("js", "application/javascript");
            MimeTypes.Add("au", "audio/basic");
            MimeTypes.Add("snd", "audio/basic");
            MimeTypes.Add("es", "audio/echospeech");
            MimeTypes.Add("mp3", "audio/mpeg");
            MimeTypes.Add("mp2", "audio/mpeg");
            MimeTypes.Add("mid", "audio/midi");
            MimeTypes.Add("wav", "audio/x-wav");
            MimeTypes.Add("swf", "application/x-shockwave-flash");
            MimeTypes.Add("avi", "video/avi");
            MimeTypes.Add("rm", "audio/x-pn-realaudio");
            MimeTypes.Add("ram", "audio/x-pn-realaudio");
            MimeTypes.Add("aif", "audio/x-aiff");
        }

        /// <summary>
        /// Determines if the request should be handled by this module.
        /// Invoked by the <see cref="HttpServer"/>
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>true if this module should handle it.</returns>
        public bool CanHandle(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                return false;

            string path = GetPath(uri);
            return
                uri.AbsolutePath.StartsWith(_baseUri) && // Correct directory
                File.Exists(path) && // File exists
                (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0; // Not a symlink
        }

        /// <exception cref="BadRequestException">Illegal path</exception>
        private string GetPath(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                throw new BadRequestException("Illegal path");

            string path = Uri.UnescapeDataString(uri.LocalPath);
            path = _basePath + path.Substring(_baseUri.Length);
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// check if source contains any of the chars.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        private static bool Contains(string source, IEnumerable<string> chars)
        {
            foreach (string s in chars)
            {
                if (source.Contains(s))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Method that process the Uri.
        /// </summary>
        /// <param name="request">Information sent by the browser about the request</param>
        /// <param name="response">Information that is being sent back to the client.</param>
        /// <param name="session">Session used to </param>
        /// <exception cref="InternalServerException">Failed to find file extension</exception>
        /// <exception cref="ForbiddenException">File type is forbidden.</exception>
        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            string state = request.QueryString["state"].Value;
            string code = request.QueryString["code"].Value;
            if (code != null && code.Length > 9 && request.Uri.LocalPath == "/")
            {
                response.Redirect("/drive?code=" + code);
                return false;
            }

            try
            {
                switch (request.Uri.LocalPath)
                {
                    case "/drive_callback":
                        if (true)
                        {

                            byte[] body = Encoding.UTF8.GetBytes(code);
                            response.Body.Write(body, 0, body.Length);
                            response.Send();
                        }
                        break;
                    case "/drive":
                        #region

                        if (true)
                        {
                            string url = "";
                            //string json = "{}";
                            if (state == null) state = "thinhtu";
                            if (code == null) code = "thinhtu";

                            try
                            {
                                IAuthenticator authenticator = Utils.GetCredentials(code, state);
                                // Store the authenticator and the authorized service in session
                                session["authenticator"] = authenticator;
                                DriveService sv = Utils.BuildService(authenticator);
                                session["service"] = sv;
                            }
                            catch (CodeExchangeException)
                            {
                                if (session["service"] == null || session["authenticator"] == null)
                                {
                                    url = Utils.GetAuthorizationUrl("", state);
                                    response.Redirect(url);
                                    return false;
                                }
                            }
                            catch (NoRefreshTokenException e)
                            {
                                url = e.AuthorizationUrl;
                                response.Redirect(url);
                                return false;
                            }

                            response.Redirect("/user");
                            return false;

                            //////DriveState driveState = new DriveState();

                            //////if (!string.IsNullOrEmpty(state))
                            //////{
                            //////    driveState = Newtonsoft.Json.JsonConvert.DeserializeObject<DriveState>(state);
                            //////}

                            //////if (driveState.action == "open")
                            //////{
                            //////    //return OpenWith(driveState);
                            //////    json = Newtonsoft.Json.JsonConvert.SerializeObject(new { FileIds = driveState.ids });
                            //////}
                            //////else
                            //////{
                            //////    //return CreateNew(driveState);
                            //////    json = Newtonsoft.Json.JsonConvert.SerializeObject(new { FileIds = new string[] { "" } });
                            //////}

                            //////response.ContentType = "application/json; charset=UTF-8";
                            //////byte[] body = Encoding.UTF8.GetBytes(json);
                            //////response.Body.Write(body, 0, body.Length);
                            //////response.Send();
                        }

                    #endregion
                    //break;
                    case "/user":
                        #region
                        if (true)
                        {
                            IAuthenticator authenticator = session["authenticator"] as IAuthenticator;
                            Userinfo userInfo = Utils.GetUserInfo(authenticator);
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { email = userInfo.Email, link = userInfo.Link, picture = userInfo.Picture });

                            response.ContentType = "application/json; charset=UTF-8";
                            byte[] body = Encoding.UTF8.GetBytes(json);
                            response.Body.Write(body, 0, body.Length);
                            response.Send();
                        }
                        #endregion
                        break;
                    case "/get_dirs":
                        #region
                        if (true)
                        {
                            string _path = request.QueryString["path"].Value;
                            string json = "[]";

                            if (!string.IsNullOrEmpty(_path))
                            {
                                _path = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), _path);
                                if (Directory.Exists(_path))
                                    json = Newtonsoft.Json.JsonConvert.SerializeObject(Directory.GetDirectories(_path).Select(x => Path.GetFileName(x)).ToArray());
                            }

                            response.ContentType = "application/json; charset=UTF-8";
                            byte[] body = Encoding.UTF8.GetBytes(json);
                            response.Body.Write(body, 0, body.Length);
                            response.Send();
                        }
                        #endregion
                        break;
                    case "/get_files":
                        #region
                        if (true)
                        {
                            string _path = request.QueryString["path"].Value;
                            string json = "[]";

                            if (!string.IsNullOrEmpty(_path))
                            {
                                _path = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), _path);
                                if (Directory.Exists(_path))
                                    json = Newtonsoft.Json.JsonConvert.SerializeObject(Directory.GetFiles(_path).Select(x => Path.GetFileName(x)).ToArray());
                            }

                            response.ContentType = "application/json; charset=UTF-8";
                            byte[] body = Encoding.UTF8.GetBytes(json);
                            response.Body.Write(body, 0, body.Length);
                            response.Send();
                        }
                        #endregion
                        break;
                    case "/about":
                        #region
                        if (true)
                        {
                            DriveService service = session["service"] as DriveService;
                            if (service == null)
                            {
                                // redirect user to authentication
                                byte[] body = Encoding.UTF8.GetBytes("Redirect user to authentication");
                                response.Body.Write(body, 0, body.Length);
                                response.Send();
                            }
                            else
                            {
                                Google.Apis.Drive.v2.Data.About about = service.About.Get().Fetch();
                                string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { quotaBytesTotal = about.QuotaBytesTotal, quotaBytesUsed = about.QuotaBytesUsed });

                                response.ContentType = "application/json; charset=UTF-8";
                                byte[] body = Encoding.UTF8.GetBytes(json);
                                response.Body.Write(body, 0, body.Length);
                                response.Send();
                            }
                        }
                        #endregion
                        break;
                    case "/favicon.ico":
                        byte[] body1 = Encoding.UTF8.GetBytes(string.Empty);
                        response.Body.Write(body1, 0, body1.Length);
                        response.Send();
                        break;
                    default:
                        #region

                        if (request.Uri.LocalPath.EndsWith("/"))
                        {
                            string pathLocal = GetPath(request.Uri);
                            string[] folders = Directory.GetDirectories(pathLocal);

                            string subPath = pathLocal.Replace(this._basePath, string.Empty).Trim();
                            if (subPath.Length > 0) subPath = subPath.Replace("\\", "/");


                            string file_index = string.Format("{0}{1}{2}", this._basePath, subPath, "index.html").Replace("/", "\\");
                            if (File.Exists(file_index) && !request.Uri.ToString().Contains("?file"))
                            {
                                string htm = File.ReadAllText(file_index);
                                byte[] body = Encoding.UTF8.GetBytes(htm);
                                response.Body.Write(body, 0, body.Length);
                                response.Send();
                            }
                            else
                            {
                                StringBuilder biHome = new StringBuilder();
                                string[] files = Directory.GetFiles(pathLocal)
                                    //.Where(x =>
                                    //    x.ToLower().EndsWith(".txt") ||
                                    //    x.ToLower().EndsWith(".md") ||
                                    //    x.ToLower().EndsWith(".json") ||
                                    //    x.ToLower().EndsWith(".html") ||
                                    //    x.ToLower().EndsWith(".htm")
                                    //)
                                    .ToArray();
                                foreach (string pi in folders) biHome.Append(string.Format("<a href=/{0}{1}/>{1}</a><br>", subPath, Path.GetFileName(pi)));
                                biHome.Append("<br>");
                                foreach (string pi in files) biHome.Append(string.Format("<a href=/{0}{1}>{1}</a><br>", subPath, Path.GetFileName(pi)));

                                byte[] body = Encoding.UTF8.GetBytes(string.Format("<h1>{0}</h1>{1}<br><hr>{2}", request.Uri.LocalPath, biHome.ToString(), DateTime.Now.ToString()));
                                response.Body.Write(body, 0, body.Length);
                                response.Send();
                            }

                            return true;
                        }

                        if (!CanHandle(request.Uri))
                            return false;

                        string path = GetPath(request.Uri);
                        string extension = GetFileExtension(path);
                        if (extension == null)
                            throw new InternalServerException("Failed to find file extension");

                        if (MimeTypes.ContainsKey(extension))
                            response.ContentType = MimeTypes[extension];
                        else
                            throw new ForbiddenException("Forbidden file type: " + extension);

                        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            if (!string.IsNullOrEmpty(request.Headers["if-Modified-Since"]))
                            {
                                DateTime since = DateTime.Parse(request.Headers["if-Modified-Since"]).ToUniversalTime();
                                DateTime modified = File.GetLastWriteTime(path).ToUniversalTime();

                                // Truncate the subsecond portion of the time stamp (if present)
                                modified = new DateTime(modified.Year, modified.Month, modified.Day, modified.Hour,
                                    modified.Minute, modified.Second, DateTimeKind.Utc);

                                if (modified > since)
                                    response.Status = HttpStatusCode.NotModified;
                            }

                            // Fixed by Albert, Team MediaPortal: ToUniversalTime
                            if (_useLastModifiedHeader)
                                response.AddHeader("Last-modified", File.GetLastWriteTime(path).ToUniversalTime().ToString("r"));
                            response.ContentLength = stream.Length;
                            response.SendHeaders();

                            if (request.Method != "Headers" && response.Status != HttpStatusCode.NotModified)
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead = stream.Read(buffer, 0, 8192);
                                while (bytesRead > 0)
                                {
                                    response.SendBody(buffer, 0, bytesRead);
                                    bytesRead = stream.Read(buffer, 0, 8192);
                                }
                            }
                        }

                        #endregion
                        break;
                }

            }
            catch (FileNotFoundException err)
            {
                throw new InternalServerException("Failed to process file.", err);
            }

            return true;
        }

        /// <summary>
        /// return a file extension from an absolute Uri path (or plain filename)
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string GetFileExtension(string uri)
        {
            int pos = uri.LastIndexOf('.');
            return pos == -1 ? null : uri.Substring(pos + 1);
        }
    }
}
