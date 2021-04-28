using host.Http;
using host.Http.HttpModules;
using host.Http.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace host
{
    class webProcess : HttpModule
    {
        /// <summary>
        /// Method that process the URL
        /// </summary>
        /// <param name="request">Information sent by the browser about the request</param>
        /// <param name="response">Information that is being sent back to the client.</param>
        /// <param name="session">Session used to </param>
        /// <returns>true if this module handled the request.</returns>
        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            string uri = request.Uri.AbsolutePath, api = "", ext = "", path = "",
                theme = hostContext.theme_Default, device = hostContext.device_Default,
                browser = "", lang = hostContext.lang_Default;

            switch (request.Method)
            {
                case Method.Post:
                    break;
                case Method.Get:
                    #region // GET ...
                    switch (uri)
                    {
                        case "/pipe":
                            webResponse.write_page(request, response, session, main.processCurrent_PID.ToString());
                            break;
                        case "/tcp":
                            webResponse.write_page(request, response, session, main.processCurrent_PID.ToString());
                            break;
                    }

                    #endregion
                    break;
            }
            
            //if (session["times"] == null)
            //    session["times"] = 1;
            //else
            //    session["times"] = ((int)session["times"]) + 1;
            //StreamWriter writer = new StreamWriter(response.Body);
            //writer.WriteLine("Hello dude, you have been here " + session["times"] + " times.");
            //writer.Flush();
            return true; // return true to tell webserver that we've handled the url
        }
    }
}
