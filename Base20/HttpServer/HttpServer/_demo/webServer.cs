using host.Http;
using host.Http.HttpModules;
using model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace host
{
    public class webServer
    {
        public static void init()
        { 
            HttpServer server = new HttpServer(); 

            FileModule module = new FileModule("/", main.config.path_module); 
            module.AddDefaultMimeTypes();
            server.Add(module);

            server.Add(new webProcess());

            server.Start(IPAddress.Parse(main.config.site_ip), main.config.site_port); 

            main.show_notification(main.page_Site, 3000);
        }
         

        private static void w_ProcessRequest(HttpListenerContext ctx)
        {
            /*
            HttpListenerRequest request = ctx.Request;
            Dictionary<string, string> postCookie = request.Cookies.Cast<Cookie>().ToDictionary(x => x.Name, x => x.Value);
            if (postCookie == null) postCookie = new Dictionary<string, string>() { };
            string uri = request.Url.AbsolutePath, api = "", ext = "", path = "",
                theme = hostContext.theme_Default, device = hostContext.device_Default,
                browser = "", lang = hostContext.lang_Default;
            if (ctx.Request.HttpMethod == "GET")
            {
                #region // GET ...
                try
                {

                    #region // CORS ...
                    if (ctx.Request.HttpMethod == "OPTIONS")
                    {
                        ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                        ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                        ctx.Response.AddHeader("Access-Control-Max-Age", "1728000");
                    }
                    ctx.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                    #endregion

                    string[] a = uri.Split('/').Where(x => x != "").ToArray();

                    #region // api, path, browser ...

                    switch (a.Length)
                    {
                        case 0: // home
                            api = "home";
                            ext = main.pathSite_Ext;
                            break;
                        case 1:
                            api = a[0];
                            a = a[a.Length - 1].Split('.');
                            path = a[0];
                            ext = a[a.Length - 1];
                            break;
                        case 2:
                            api = a[0];
                            path = a[0] + @"/" + a[1];
                            a = a[a.Length - 1].Split('.');
                            ext = a[a.Length - 1];
                            break;
                        case 3:
                            api = a[0];
                            path = a[0] + @"/" + a[1] + @"/" + a[2];
                            a = a[a.Length - 1].Split('.');
                            ext = a[a.Length - 1];
                            break;
                        default:
                            api = "file";
                            path = uri.Substring(1, uri.Length - 1);
                            a = a[a.Length - 1].Split('.');
                            ext = a[a.Length - 1];
                            break;
                    }

                    switch (api[0])
                    {
                        case '.':
                            api = "module";
                            break;
                        case '_':
                            api = "layout";
                            break;
                    }

                    //if (request.Cookies["browser_name"].Value == null)
                    //{

                    string ua = request.UserAgent.ToLower();
                    if (ua.Contains("chrome")) browser = "chrome";
                    else if (ua.Contains("safari")) browser = "safari";
                    else if (ua.Contains("firefox")) browser = "firefox";
                    else if (ua.Contains("edgehtml")) browser = "edge";
                    else if (ua.Contains("msie 11")) browser = "ie11";
                    else if (ua.Contains("msie 10")) browser = "ie10";
                    else if (ua.Contains("msie 9")) browser = "ie9";
                    else if (ua.Contains("msie 8")) browser = "ie8";
                    else if (ua.Contains("msie 7")) browser = "ie7";
                    //}
                    //else
                    //    browser = request.Cookies["browser_name"].Value;

                    #endregion

                    string s = "", key = path;// SendResponse(ctx.Request);
                    m_cache item;

                    switch (api)
                    {
                        case "favicon.ico":
                            break;
                        case "home":
                        case "index":
                            #region
                            if (ext == main.pathSite_Ext)
                                key = "index/index_" + device + ".html?" + theme + "." + device + "." + browser + "." + lang;
                            else
                            {
                                if (ext == "htm" || ext == "html" || ext == "js" || ext == "css" || ext == "txt" || ext == "json")
                                    key += "?" + browser + "." + lang;
                            }

                            item = httpCache.get(key);

                            if (item.Content != null && item.Content.Length > 0)
                                webResponse.write_Page(ctx.Response, item);

                            break;
                            #endregion
                        case "layout":
                            key = key + "?" + browser + "." + lang;
                            item = httpCache.get(key);
                            if (item.Content != null && item.Content.Length > 0)
                                webResponse.write_Page(ctx.Response, item);
                            break;
                        case "module":
                            #region

                            if (ext == main.pathSite_Ext)
                                key = "index/index_.html?" + theme + "." + device + "." + browser + "." + lang;
                            else
                            {
                                if (ext == "htm" || ext == "html" || ext == "js" || ext == "css" || ext == "txt" || ext == "json")
                                    key += "?" + browser + "." + lang;
                            }
                            item = httpCache.get(key);
                            if (item.Content != null && item.Content.Length > 0)
                                webResponse.write_Page(ctx.Response, item);

                            break;
                            #endregion
                        case "key":
                            string m_key_like = request.QueryString["like"];
                            string[] ls_keys = httpCache.get_Keys();

                            if (!string.IsNullOrWhiteSpace(m_key_like))
                                ls_keys = ls_keys.Where(x => x.Contains(m_key_like)).ToArray();

                            StringBuilder bi_key = new StringBuilder();
                            foreach (string li in ls_keys)
                                bi_key.Append(@"<a href=""" + li + @""" target=""_blank"">" + li + "</a><br>");

                            s = bi_key.ToString();
                            webResponse.write_PageBody(ctx.Response, s);
                            break;
                        case "file":
                            if (ext == "htm" || ext == "html" || ext == "js" || ext == "css" || ext == "txt" || ext == "json")
                                key += "?" + browser + "." + lang;

                            item = httpCache.get(key);
                            if (item.Content != null && item.Content.Length > 0)
                            {
                                ctx.Response.ContentType = item.MimeType;
                                ctx.Response.ContentLength64 = item.Content.Length;

                                Stream OutputStream = ctx.Response.OutputStream;
                                OutputStream.Write(item.Content, 0, item.Content.Length);
                                OutputStream.Close();
                            }
                            break;
                        default: // page
                            #region
                            if (ext == main.pathSite_Ext || ext == "html")
                                key = path + "/index_" + device + ".html?" + theme + "." + device + "." + browser + "." + lang;
                            else
                            {
                                if (ext == "htm" || ext == "js" || ext == "css" || ext == "txt" || ext == "json")
                                    key += "?" + browser + "." + lang;
                            }

                            item = httpCache.get(key);
                            if (item.Content != null && item.Content.Length > 0)
                                webResponse.write_Page(ctx.Response, item);
                            break;
                            #endregion
                    }
                }
                catch (Exception ex)
                {
                    webResponse.write_PageBody(ctx.Response, ex.Message);
                } // suppress any exceptions
                finally
                {
                    // always close the stream
                    ctx.Response.OutputStream.Close();
                }
                #endregion
            }
            else
            {
                #region // POST, PUT, DELETE ...

                Dictionary<string, string> postData = new Dictionary<string, string>();

                #region // Query String, Form ...

                if (request.HasEntityBody)
                {
                    using (var bodyStream = request.InputStream)
                    using (var streamReader = new StreamReader(bodyStream, request.ContentEncoding))
                    {
                        if (request.ContentLength64 > 0)
                        {
                            string rawData = streamReader.ReadToEnd();

                            string[] rawParams = rawData.Split('&');
                            foreach (string param in rawParams)
                            {
                                string[] kvPair = param.Split('=');
                                string key = kvPair[0];
                                string value = HttpUtility.UrlDecode(kvPair[1]);
                                if (!postData.ContainsKey(key))
                                    postData.Add(key, value);
                            }
                        }
                    }
                }

                #endregion

                switch (uri)
                {
                    case "/login":
                        #region
                        string username = "", pass = "";
                        if (postData.TryGetValue("username", out username) && postData.TryGetValue("pass", out pass))
                        {
                            m_user u = db_user.login(new m_user() { username = username, pass = pass });
                            if (u.status == false)
                            {
                                //return 401;
                                webResponse.write_PageRedirect(ctx.Response, "/?msg=Vui+l%C3%B2ng+%C4%91%C4%83ng+nh%E1%BA%ADp+t%C3%A0i+kho%E1%BA%A3n+ch%C3%ADnh+x%C3%A1c");
                                return;
                            }

                            string host = request.Url.Host;

                            var time = DateTime.UtcNow.AddDays(7);

                            string token = "";
                            var dui = new Dictionary<string, object>();
                            dui.Add(ClaimTypes.Name, u.username);

                            var jwttoken = new JwtToken()
                            {
                                Issuer = "http://" + host,
                                Audience = "http://" + host,
                                Claims = new Dictionary<string, object>[] { dui },
                                Expiry = time
                            };

                            token = JsonWebToken.Encode(jwttoken, tokenLogin.securekey, JwtHashAlgorithm.HS256);

                            hostContext.tokenAdd(u.username, token, time);

                            string session_id = "", browser_width = "", device_type = "";
                            if (postCookie.TryGetValue("session_id", out session_id))
                            {
                                postCookie.TryGetValue("browser_width", out browser_width);
                                postCookie.TryGetValue("device_type", out device_type);

                                hostContext.session_BrowserWidth_Set(session_id, browser_width.TryParseToInt());
                                hostContext.session_deviceType_Set(session_id, device_type);
                            }

                            Dictionary<string, string> pageCookie = new Dictionary<string, string>() { };
                            pageCookie.Add("username", username);
                            pageCookie.Add("token_id", token);

                            webResponse.write_PageRedirect(ctx.Response, main.page_Site_Main, pageCookie);
                            return;
                        }
                        else
                        {
                            //return 401;
                            webResponse.write_PageRedirect(ctx.Response, "/?msg=Vui+l%C3%B2ng+%C4%91%C4%83ng+nh%E1%BA%ADp+t%C3%A0i+kho%E1%BA%A3n+ch%C3%ADnh+x%C3%A1c");
                            return;
                        }
                        #endregion
                }

                #endregion
            }
             */
        }//end main function


    }
}
