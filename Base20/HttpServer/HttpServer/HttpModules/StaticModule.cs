using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using host.Http.Exceptions;
using host.Http.Sessions;
using System.Text;
using System.Linq;

namespace host.Http.HttpModules
{
    public class StaticModule : HttpModule
    {
        private readonly string _baseUri;
        private readonly string _basePath;
        private readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();
        private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

        public StaticModule(string baseUri, string basePath)
        {
            Check.Require(baseUri, "baseUri");
            Check.Require(basePath, "basePath");

            _baseUri = baseUri;
            _basePath = basePath;
            if (!_basePath.EndsWith(PathSeparator)) _basePath += PathSeparator;

            if (_mimeTypes.Count == 0) AddDefaultMimeTypes();
        }

        public IDictionary<string, string> MimeTypes { get { return _mimeTypes; } }

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
         
        /// <exception cref="BadRequestException">Illegal path</exception>
        private string GetPath(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                throw new BadRequestException("Illegal path");

            string path = Uri.UnescapeDataString(uri.LocalPath);
            path = _basePath + path.Substring(_baseUri.Length);
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            try
            {
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
