using SWAT.Apps.WebinarRecordingsViewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SWAT.Apps.WebinarRecordingsViewer.Implementation.Common
{
    public class FileHost : IFileHost
    {

        private long Port { get; set; }
        private string MimeType { get; set; }
        private PointerRecord PointerRecord { get; set; }
        public string FileName { get; set; }
        public string ResultingFilename { get; set; }

        public virtual string Url
        {
            get
            {
                return "http://localhost:" + this.Port + "/";
            }
        }

        public Guid Guid { get; set; }

        public FileHost(string filename, PointerRecord pointerRecord, string mimeType, long port, string resultingFilename, string realurl)
        {
            this.Port = port;
            this.MimeType = mimeType;
            this.FileName = filename;
            this.PointerRecord = pointerRecord;
            this.ResultingFilename = resultingFilename;
            this.RealUrl = realurl;
            this.Guid = Guid.NewGuid();
        }


        public void Dispose()
        {
            this.Stop();
        }


        private bool Stopped { get; set; }
        private bool Stopping { get; set; }

        public void Stop()
        {
            if (!Stopped)
            {
                Stopping = true;
                if (this.listener != null)
                {
                    this.listener.Abort();

                }

            }
            this.listener.Close();


        }
        public HttpListener listener { get; set; }


        public void Start()
        {
            Stopped = false;
            Stopping = false;

            this.listener = new HttpListener();
            this.listener.Prefixes.Add(string.Format("http://localhost:{0}/", this.Port));
            this.listener.Start();
            IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), this.listener);
        }



        public void ListenerCallback(IAsyncResult result)
        {
            if (this.listener == null)
                return;
            HttpListenerContext context = this.listener.EndGetContext(result);

            // Call EndGetContext to complete the asynchronous operation.
            if (!Stopping)
            {
                this.listener.BeginGetContext(new AsyncCallback(ListenerCallback), this.listener);
                this.WriteFile(context.Response, context.Request);

            }
            if (Stopping)
            {
                Stopped = true;
                Stopping = false;

            }

        }

        public string RealUrl { get; set; }

        private void WriteFile(HttpListenerResponse response, HttpListenerRequest request)
        {
            using (FileStream fs = File.Open(/*this.FileName*/"f:\\work\\download.wmv", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                response.KeepAlive = true;
                response.SendChunked = true;
                bool doingRange = false;
                //      response.AddHeader("Last-Modified", "Sun, 14 Nov 2010 21:15:21 GMT");
                //   response.ProtocolVersion = new Version("1.1");
                //       response.AddHeader("Cache-Control", "max-age=86400");
                //        response.AddHeader("ETag", string.Format("\"{0}\"", this.Guid.ToString()));
                response.AddHeader("Accept-Ranges", "bytes");
                response.ContentType = this.MimeType;
                long start = 0;
                long length = fs.Length;//this.PointerRecord.Length;
                Int64 endByte = length - 1;
                if (request.Headers["range"] != null)
                {
                    doingRange = true;
                    start = long.Parse(request.Headers["range"].Split('=')[1].Split('-')[0].Trim());
                    if ((request.Headers["range"].Split('=')[1].Split('-').Length > 1))
                    {
                        Int64 endread = 0;
                        Int64.TryParse(request.Headers["range"].Split('=')[1].Split('-')[1].Trim(), out endread);
                        if (endread > 0)
                        {
                            endByte = endread;
                        }

                    }
                    if ((endByte + 1) > this.PointerRecord.Length)
                    {
                        endByte = this.PointerRecord.Length - 1;
                    }

                    length = (endByte - start) + 1;
                    response.AddHeader("Content-Range", "bytes=" + start.ToString() + "-" + endByte.ToString() + "/" + fs.Length.ToString());
                    response.ContentLength64 = length;

                }
                else
                {
                    response.ContentLength64 = fs.Length;


                }

                byte[] buffer = new byte[1024 * 1024];

                fs.Seek(start, SeekOrigin.Begin);

                long read = 0;
                long runningTotal = 0;
                try
                {
                    var stream = response.OutputStream;
                    while ((length > 0) && ((read = fs.Read(buffer, 0, buffer.Length)) > 0))
                    {
                        runningTotal += read;
                        // if the read overshoots the requested read length
                        if (runningTotal > length)
                        {
                            read = read - (runningTotal - length);
                        }
                        stream.Write(buffer, 0, Convert.ToInt32(read));
                        stream.Flush();
                        if (runningTotal >= length)
                        {
                            break;
                        }



                    }
                    stream.Close();
                    response.StatusCode = doingRange ? (int)HttpStatusCode.PartialContent : (int)HttpStatusCode.OK;
                    response.StatusDescription = (doingRange ? HttpStatusCode.PartialContent : HttpStatusCode.OK).ToString();

                }
                catch (Exception ex)
                {
                    var test = ex.Message;


                }
                finally
                {
                    fs.Close();
                    fs.Dispose();
                }


            }
        }




        public event EventHandler<FileHostEventArgs> Error;

        public void OnError(FileDownloadEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}