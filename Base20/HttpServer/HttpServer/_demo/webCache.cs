using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using model;

namespace host
{
    public class httpCache
    {
        private static readonly Regex regEx_IncludeModule = new
            Regex(@"@Module\['(?<ViewName>[^\]]+)'(?:.[ ]?@?(?<Model>(Model|Current)(?:\.(?<ParameterName>[a-zA-Z0-9-_]+))*))?\];?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex regEx_Form = new
            Regex(@"<(?<Tag_Name>(form))\b[^>]*?\b(?<PostUrl>post)\s*=\s*(?:""(?<URL>(?:\\""|[^""])*)""|'(?<URL>(?:\\'|[^'])*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex regEx_ResourceURL = new
            //Regex(@"<(?<Tag_Name>(a)|img|link|script)\b[^>]*?\b(?<URL_Type>(?(1)href|src))\s*=\s*(?:""(?<URL>(?:\\""|[^""])*)""|'(?<URL>(?:\\'|[^'])*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex(@"<(?<Tag_Name>(a)|iframe|img|link|script)\b[^>]*?\b(?<URL_Type>src|href)\s*=\s*(?:""(?<URL>(?:\\""|[^""])*)""|'(?<URL>(?:\\'|[^'])*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static List<char> ls_type = new List<char>() { '.', '_' };
        private static string[] a_type = new string[] { "module", "layout" };

        //private static string[] a_browser = new string[] { "chrome", "firefox", "sanfari", "edge", "ie11", "ie10", "ie9", "ie9", "ie8", "ie7", "ie6" };
        private static string[] a_browser = new string[] { "chrome", "firefox" };
        private static List<string> ls_device = new List<string>() { "pc", "tablet", "mobi" };
        private static List<string> ls_theme = new List<string>() { "dark", "light" };
        private static List<string> ls_lang = new List<string>() { "vi", "en" };

        private static List<string> ls_key = new List<string>() { };
        private static List<string> ls_file = new List<string>() { };
        private static ObjectCache cache = MemoryCache.Default;
        private static CacheItemPolicy policy = new CacheItemPolicy();

        public static void init()
        {
            policy.Priority = System.Runtime.Caching.CacheItemPriority.Default;
            policy.AbsoluteExpiration = DateTimeOffset.Now.AddDays(30);

            layout_Cache();
            module_Cache(main.pathModule);
            site_Cache();
        }

        public static string[] get_Keys()
        {
            return ls_key.ToArray();
        }

        public static string[] get_Files()
        {
            return ls_file.ToArray();
        }

        #region //  cache: set; get; getString ...


        public static void set(m_cache item)
        {
            //byte[] buffer = Encoding.UTF8.GetBytes(data);
            //Muốn tạo sự kiện thì tạo ở đây còn k thì hoy
            //(MyCacheItemPriority == MyCachePriority.Default) ? CacheItemPriority.Default : CacheItemPriority.NotRemovable;
            //Globals.policy.RemovedCallback = callback; 
            if (!string.IsNullOrEmpty(item.Key) && item.Content != null && item.Content.Length > 0)
            {
                cache.Set(item.Key, item, policy, null);
                ls_key.Add(item.Key);
                if (ls_file.IndexOf(item.PathFile) == -1) ls_file.Add(item.PathFile);
            }
        }

        private static string f_format_htm(string htm, m_cache item)
        {
            return htm.Replace("../", string.Empty)
                .Replace("___host_connect", main.page_Socket)
                .Replace("___modkey", item.modkey)
                .Replace("___browser_name", item.browser_name)
                .Replace("___theme_key", item.theme_key)
                .Replace("___device_type", item.device_type)
                .Replace("___lang_key", item.lang_key);
        }

        private static string f_format_CssJs(string css_js, m_cache item)
        {
            return css_js
                .Replace("___host_connect", main.page_Socket)
                .Replace("___modkey", item.modkey)
                .Replace("___browser_name", item.browser_name)
                .Replace("___theme_key", item.theme_key)
                .Replace("___device_type", item.device_type)
                .Replace("___lang_key", item.lang_key);
        }

        public static void set(string file, string path_root)
        {

            string type_code = "", name = "", modkey = "", ext = "", key = "", folder_root = "", path_dir = "", path_key = file.Replace(path_root, string.Empty);
            path_key = path_key.Replace("\\", "/");
            if (path_key.IndexOf("/") == 0) path_key = path_key.Substring(1, path_key.Length - 1);

            switch (path_key[0])
            {
                case '.':
                    type_code = "module";
                    break;
                case '_':
                    type_code = "layout";
                    break;
                default:
                    type_code = "page";
                    break;
            }

            if (file.EndsWith(".jpg"))
                name = "";

            string[] a = path_key.Split('/');
            if (a.Length < 2) return;

            folder_root = a[0];
            name = a[a.Length - 1];
            path_dir = path_key.Substring(0, path_key.Length - (name.Length + 1));
            modkey = path_dir.Replace(".", string.Empty).Replace("/", "___")
               .Replace(' ', '_').Replace('-', '_').Replace(".", string.Empty);
            a = name.Split(new string[] { "_", "." }, StringSplitOptions.None);
            ext = a[a.Length - 1];

            m_cache f = new m_cache();
            f.theme_key = hostContext.theme_Default;
            f.device_type = hostContext.device_Default;
            f.lang_key = hostContext.lang_Default;
            f.browser_name = "chrome";
            f.Type = type_code;
            f.Cache = true;
            f.Ext = ext;
            f.Name = path_key;
            f.MimeType = mineType.GetContentType(ext);
            f.PathFile = file;

            if (ext == "htm" || ext == "html" || ext == "js" || ext == "css" || ext == "txt" || ext == "json")
            {
                StreamReader streamReader = new StreamReader(file);
                string s = streamReader.ReadToEnd();

                if (a.Length > 3)
                {
                    string theme = a[a.Length - 3], device = a[a.Length - 2];
                    if (ls_device.IndexOf(device) != -1 && ls_theme.IndexOf(theme) != -1)
                        s = s.Replace("___theme_key", theme).Replace("___device_type", device);
                }
                s = s.Replace("___modkey", modkey);

                switch (type_code)
                {
                    case "module":
                        #region

                        switch (ext)
                        {
                            case "html":
                                #region
                                var ls_uri = regEx_ResourceURL.Matches(s).Cast<Match>()
                                    .Select(m => new { tag = m.ToString(), uri = m.Groups["URL"].Value.Replace(@"\", "/") })
                                    .Where(x => !x.uri.Contains(main.pathSite_Ext))
                                    .ToArray();

                                if (modkey.IndexOf("api___") != 0)
                                {
                                    s = "<div id='" + modkey + "'><div class='mod " + modkey + " module__' mdir='" + path_dir + "' mkey='" + path_key + "'>" + Environment.NewLine +
                                        @"<em class='btnConfig fa fa-wrench @username' onclick=""apiConfig('" + path_key + @"')""></em>" + Environment.NewLine + s + Environment.NewLine + "</div></div>";
                                }

                                foreach (string theme in ls_theme)
                                {
                                    foreach (string device in ls_device)
                                    {
                                        foreach (string bi in a_browser)
                                        {
                                            #region // uri format ...

                                            if (ls_uri.Length > 0)
                                            {
                                                foreach (var ri in ls_uri)
                                                {
                                                    string tag = ri.tag, uri = ri.uri, tag_new = "", uri_new = "";
                                                    if (uri.StartsWith("../"))
                                                    {
                                                        uri_new = "/" + uri.Replace("../", string.Empty);
                                                        tag_new = tag.Replace(uri, uri_new.ToLower());
                                                        s = s.Replace(tag, tag_new);
                                                    }
                                                    else
                                                    {
                                                        if (!uri.StartsWith("http") && !uri.StartsWith("/" + folder_root))
                                                        {
                                                            uri_new = "/" + path_dir + "/" + uri;
                                                            tag_new = tag.Replace(uri, uri_new.ToLower());
                                                            s = s.Replace(tag, tag_new);
                                                        }
                                                    }
                                                }
                                            }

                                            #endregion

                                            string dir_lang = path_root + "\\" + path_dir + "\\lang\\";
                                            dir_lang = dir_lang.Replace("/", "\\").Replace("\\\\", "\\");
                                            foreach (string lang in ls_lang)
                                            {
                                                f.modkey = modkey;
                                                f.browser_name = bi;
                                                f.theme_key = theme;
                                                f.lang_key = lang;
                                                f.device_type = device;
                                                string si = f_format_htm(s, f);

                                                #region // json/lang: vi, en ...

                                                string fla = dir_lang + lang + ".json";
                                                if (File.Exists(fla))
                                                {
                                                    string s_la = File.ReadAllText(fla);

                                                    var las = s_la.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                                        .Select(x => x.Trim())
                                                        .Where(x => x.Contains(":"))
                                                        .Select(x => new Tuple<string, string>(
                                                            "[lang_" + x.Split(':')[0].Trim() + "]",
                                                            x.Split(':')[1].Replace(@"""", string.Empty).Replace(",", string.Empty).Trim()))
                                                            .ToArray();

                                                    foreach (var lai in las)
                                                        si = si.Replace(lai.Item1, lai.Item2);
                                                }

                                                #endregion

                                                key = path_key + "?" + theme + "." + device + "." + bi + "." + lang;
                                                si = si +
                                                    Environment.NewLine + @"<link href=""/" + path_dir + "/bin_" + theme + "_" + device + @".css"" rel=""stylesheet"" /> " +
                                                    Environment.NewLine + @"<script src=""/" + path_dir + "/bin_" + theme + "_" + device + @".js""></script>";

                                                f.Key = key;
                                                f.Content = Encoding.UTF8.GetBytes(si);
                                                set(f);
                                            } // end for lang


                                        } // end for browser
                                    } // end for device
                                } // end for theme

                                break;
                                #endregion
                            default:
                                #region // css, js, json, txt ..,

                                foreach (string bi in a_browser)
                                {
                                    string dir_lang = path_root + "\\" + path_dir + "\\lang\\";
                                    dir_lang = dir_lang.Replace("/", "\\").Replace("\\\\", "\\");
                                    foreach (string lang in ls_lang)
                                    {
                                        string fla = dir_lang + lang + ".json";
                                        if (File.Exists(fla))
                                        {
                                            string s_la = File.ReadAllText(fla);

                                            var las = s_la.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                                .Select(x => x.Trim())
                                                .Where(x => x.Contains(":"))
                                                .Select(x => new Tuple<string, string>(
                                                    "[lang_" + x.Split(':')[0].Trim() + "]",
                                                    x.Split(':')[1].Replace(@"""", string.Empty).Replace(",", string.Empty).Trim()))
                                                    .ToArray();

                                            foreach (var lai in las)
                                                s = s.Replace(lai.Item1, lai.Item2);
                                        }

                                        key = path_key + "?" + bi + "." + lang;
                                        f.Key = key;
                                        f.Content = Encoding.UTF8.GetBytes(s);
                                        set(f);

                                    } // end for lang 
                                }// end for browser
                                break;

                                #endregion
                        }

                        break;
                        #endregion
                    case "page":
                        #region
                        switch (ext)
                        {
                            case "html":
                                #region
                                var ls_mod = regEx_IncludeModule.Matches(s).Cast<Match>()
                                    .Select(m => new
                                    {
                                        tag = m.ToString(),
                                        mod_path = m.Groups["ViewName"].Value.ToLower()
                                    })
                                    .Distinct().ToArray();

                                var ls_uri = regEx_ResourceURL.Matches(s).Cast<Match>()
                                    .Select(m => new { tag = m.ToString(), uri = m.Groups["URL"].Value.Replace(@"\", "/") })
                                    .Where(x => !x.uri.Contains(main.pathSite_Ext)).ToArray();

                                int pos = s.ToLower().IndexOf("<head>");
                                if (pos > 0) pos = pos + 6;

                                string[] a_dv = name.Split(new string[] { "_", "." }, StringSplitOptions.None).Where(x => x != "").ToArray();
                                if (a_dv.Length > 2)
                                {
                                    string device = a_dv[a_dv.Length - 2];
                                    foreach (string theme in ls_theme)
                                    {
                                        foreach (string bi in a_browser)
                                        {
                                            #region // uri format ...

                                            if (ls_uri.Length > 0)
                                            {
                                                foreach (var ri in ls_uri)
                                                {
                                                    string tag = ri.tag, uri = ri.uri, tag_new = "", uri_new = "";
                                                    if (uri.StartsWith("../"))
                                                    {
                                                        uri_new = "/" + uri.Replace("../", string.Empty);
                                                        tag_new = tag.Replace(uri, uri_new.ToLower());
                                                        s = s.Replace(tag, tag_new);
                                                    }
                                                    else
                                                    {
                                                        switch (uri[0])
                                                        {
                                                            case '.':
                                                                uri_new = "/" + uri;
                                                                tag_new = tag.Replace(uri, uri_new.ToLower());
                                                                s = s.Replace(tag, tag_new);
                                                                break;
                                                            case '_':
                                                                uri_new = "/" + uri.ToLower();
                                                                if (!uri_new.StartsWith("/_layout/")) uri_new = "/_layout" + uri_new.Replace("/_", "/");
                                                                tag_new = tag.Replace(uri, uri_new);
                                                                s = s.Replace(tag, tag_new);
                                                                break;
                                                            default:
                                                                if (!uri.StartsWith("http") && !uri.StartsWith("/" + folder_root))
                                                                {
                                                                    uri_new = "/" + path_dir + "/" + uri;
                                                                    tag_new = tag.Replace(uri, uri_new.ToLower());
                                                                    s = s.Replace(tag, tag_new);
                                                                }
                                                                break;
                                                        }
                                                    }
                                                }
                                            }

                                            #endregion

                                            if (pos > 0)
                                                s = s.Substring(0, pos) + Environment.NewLine +
                                                        @"<script src=""/.api/init/init.js""></script>" +
                                                        Environment.NewLine + s.Substring(pos, s.Length - pos);

                                            string dir_lang = path_root + "\\" + path_dir + "\\lang\\";
                                            dir_lang = dir_lang.Replace("/", "\\").Replace("\\\\", "\\");
                                            foreach (string lang in ls_lang)
                                            {
                                                f.modkey = modkey;
                                                f.browser_name = bi;
                                                f.theme_key = theme;
                                                f.lang_key = lang;
                                                f.device_type = device;
                                                string si_lang = f_format_htm(s, f);

                                                #region // @Module ...

                                                if (ls_mod.Length > 0)
                                                {
                                                    foreach (var mi in ls_mod)
                                                    {
                                                        string tag = mi.tag, key_mod = "." + mi.mod_path + "/index_.html?" + theme + "." + device + "." + bi + "." + lang;
                                                        string s_mod = getString(key_mod);
                                                        si_lang = si_lang.Replace(tag, s_mod);
                                                    }
                                                }

                                                if (si_lang.Contains("@Module") || si_lang.Contains("@module"))
                                                {
                                                    var ls_mod2 = regEx_IncludeModule.Matches(si_lang).Cast<Match>()
                                                        .Select(m => new
                                                        {
                                                            tag = m.ToString(),
                                                            mod_path = m.Groups["ViewName"].Value.ToLower()
                                                        }).Distinct().ToArray();

                                                    if (ls_mod2.Length > 0)
                                                    {
                                                        foreach (var mi in ls_mod2)
                                                        {
                                                            string tag = mi.tag, key_mod = "." + mi.mod_path + "/index_.html?" + theme + "." + device + "." + bi + "." + lang;
                                                            string s_mod = getString(key_mod);
                                                            si_lang = si_lang.Replace(tag, s_mod);
                                                        }
                                                    }
                                                }

                                                #endregion

                                                #region // json/lang: vi, en ...

                                                string fla = dir_lang + lang + ".json";
                                                if (File.Exists(fla))
                                                {
                                                    string s_la = File.ReadAllText(fla);

                                                    var las = s_la.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                                        .Select(x => x.Trim())
                                                        .Where(x => x.Contains(":"))
                                                        .Select(x => new Tuple<string, string>(
                                                            "[lang_" + x.Split(':')[0].Trim() + "]",
                                                            x.Split(':')[1].Replace(@"""", string.Empty).Replace(",", string.Empty).Trim()))
                                                            .ToArray();

                                                    foreach (var lai in las)
                                                        si_lang = si_lang.Replace(lai.Item1, lai.Item2);
                                                }

                                                #endregion

                                                key = path_key + "?" + theme + "." + device + "." + bi + "." + lang;
                                                f.Key = key;
                                                f.Content = Encoding.UTF8.GetBytes(si_lang);
                                                set(f);
                                            } // end for lang
                                        } // end for browser 
                                    } // end for theme
                                }
                                break;
                                #endregion
                            default:
                                #region // css, js, json, txt ..,
                                foreach (string theme in ls_theme)
                                    foreach (string device in ls_device)
                                        foreach (string bi in a_browser)
                                        {
                                            string dir_lang = path_root + "\\" + path_dir + "\\lang\\";
                                            dir_lang = dir_lang.Replace("/", "\\").Replace("\\\\", "\\");
                                            foreach (string lang in ls_lang)
                                            {
                                                f.modkey = modkey;
                                                f.browser_name = bi;
                                                f.theme_key = theme;
                                                f.lang_key = lang;
                                                f.device_type = device;
                                                string si_lang = f_format_htm(s, f);

                                                #region // json/lang: vi, en ...

                                                string fla = dir_lang + lang + ".json";
                                                if (File.Exists(fla))
                                                {
                                                    string s_la = File.ReadAllText(fla);

                                                    var las = s_la.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                                        .Select(x => x.Trim())
                                                        .Where(x => x.Contains(":"))
                                                        .Select(x => new Tuple<string, string>(
                                                            "[lang_" + x.Split(':')[0].Trim() + "]",
                                                            x.Split(':')[1].Replace(@"""", string.Empty).Replace(",", string.Empty).Trim()))
                                                            .ToArray();

                                                    foreach (var lai in las)
                                                        si_lang = si_lang.Replace(lai.Item1, lai.Item2);
                                                }

                                                #endregion

                                                key = path_key + "?" + theme + "." + device + "." + bi + "." + lang;
                                                f.Key = key;
                                                f.Content = Encoding.UTF8.GetBytes(si_lang);
                                                set(f);
                                            } // end for lang 
                                        }// end for browser
                                break;

                                #endregion
                        }
                        break;
                        #endregion
                    default:
                        #region

                        foreach (string bi in a_browser)
                        {
                            string dir_lang = path_root + "\\" + path_dir + "\\lang\\";
                            dir_lang = dir_lang.Replace("/", "\\").Replace("\\\\", "\\");
                            foreach (string lang in ls_lang)
                            {
                                string fla = dir_lang + lang + ".json";
                                if (File.Exists(fla))
                                {
                                    string s_la = File.ReadAllText(fla);

                                    var las = s_la.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                        .Select(x => x.Trim())
                                        .Where(x => x.Contains(":"))
                                        .Select(x => new Tuple<string, string>(
                                            "[lang_" + x.Split(':')[0].Trim() + "]",
                                            x.Split(':')[1].Replace(@"""", string.Empty).Replace(",", string.Empty).Trim()))
                                            .ToArray();

                                    foreach (var lai in las)
                                        s = s.Replace(lai.Item1, lai.Item2);
                                }

                                key = path_key + "?" + bi + "." + lang;
                                f.Key = key;
                                f.Content = Encoding.UTF8.GetBytes(s);
                                set(f);

                            } // end for lang 
                        }// end for browser
                        break;

                        #endregion
                }
            }
            else
            {
                #region // gif, png, jpg, jpeg ...

                if (ext == "gif" || ext == "png" || ext == "jpg" || ext == "jpeg")
                {
                    ImageFormat imgFormat = ImageFormat.Jpeg;
                    switch (ext)
                    {
                        case "gif":
                            imgFormat = ImageFormat.Gif;
                            break;
                        case "png":
                            imgFormat = ImageFormat.Png;
                            break;
                        case "jpg":
                        case "jpeg":
                            imgFormat = ImageFormat.Jpeg;
                            break;
                    }

                    Image img = Image.FromFile(file);
                    byte[] buffer;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, imgFormat);
                        buffer = ms.ToArray();
                    }

                    f.Key = path_key;
                    f.Content = buffer;
                    set(f);
                }
                else
                {
                    //woff
                    // this method is limited to 2^32 byte files (4.2 GB)
                    byte[] buffer;
                    FileStream fs = null;
                    try
                    {
                        fs = File.OpenRead(file);
                        buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, Convert.ToInt32(fs.Length));
                    }
                    finally
                    {
                        if (fs != null)
                        {
                            fs.Close();
                            fs.Dispose();
                        }
                    }

                    f.Key = path_key;
                    f.Content = buffer;
                    set(f);
                }

                #endregion
            }

            ls_file.Add(file);
        }

        public static m_cache get(string key)
        {
            m_cache item = new m_cache() { };
            if (cache[key] != null) item = (m_cache)cache[key];
            return item;
        }

        public static string getString(string key)
        {
            string s = "";

            m_cache item = new m_cache() { };
            if (cache[key] != null)
            {
                item = (m_cache)cache[key];
                if (item.Content != null && item.Content.Length > 0)
                    s = Encoding.UTF8.GetString(item.Content);
            }
            return s;
        }

        #endregion


        private static void file_Cache(string path_dir, string path_root, string file_Pattern = "*.*")
        {
            var dfs = Directory.GetFiles(path_dir, file_Pattern)
                .Select(x => x.ToLower())
                .ToArray();
            if (dfs.Length > 0)
                foreach (string f in dfs)
                    set(f, path_root);

            var ds_subDir = Directory.GetDirectories(path_dir)
                .Select(x => x.ToLower())
                .ToArray();
            if (ds_subDir.Length > 0)
                foreach (string li in ds_subDir)
                    file_Cache(li, path_root);
        }

        private static void layout_Cache()
        {
            string path_layout = main.pathModule + "\\_layout";
            path_layout = path_layout.Replace("\\\\", "\\");

            if (!Directory.Exists(path_layout)) return;

            var dsLayouts = Directory.GetDirectories(path_layout)
                .Select(x => x.ToLower())
                .Where(x => x.Contains("\\_"))
                .ToArray();
            foreach (string li in dsLayouts)
                file_Cache(li, main.pathModule);
        }

        private static void module_Cache(string path)
        {
            if (!Directory.Exists(path)) return;

            var drg = Directory.GetDirectories(path)
                .Select(x => x.ToLower())
                .Where(x => x.Contains("\\."))
                .ToArray();
            if (drg.Length > 0)
            {
                foreach (string lr in drg)
                {
                    var ds = Directory.GetDirectories(lr)
                        .Select(x => x.ToLower())
                        .ToArray();
                    foreach (string li in ds)
                    {
                        file_Cache(li, main.pathModule);
                        module_Cache(li);
                    }

                    file_Cache(lr, main.pathModule);
                }
            }

        }


        private static void site_Cache()
        {
            if (!Directory.Exists(main.pathSite)) return;

            var drg = Directory.GetDirectories(main.pathSite)
                .Select(x => x.ToLower())
                .Where(x => !x.Contains("\\.") && !x.Contains("\\_"))
                .ToArray();
            foreach (string lr in drg)
            {
                var ds = Directory.GetDirectories(lr)
                    .Select(x => x.ToLower())
                    .Where(x => !x.EndsWith("\\lang"))
                    .ToArray();
                foreach (string li in ds)
                    file_Cache(li, main.pathSite);

                file_Cache(lr, main.pathSite, "*.html");
            }
        }
















































    }//end class

}
