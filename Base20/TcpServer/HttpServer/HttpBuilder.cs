namespace System.TcpHandler.Http
{
    class HttpBuilder
    {
        public static HttpResponse InternalServerError()
        {
            //string content = File.ReadAllText("Resources/Pages/500.html"); 
            return new HttpResponse()
            {
                ReasonPhrase = "InternalServerError",
                StatusCode = "500",
                ContentAsUTF8 = "<h1>Internal Server Error</h1><small>by SimpleHtppServer</small>"
            };
        }

        public static HttpResponse NotFound()
        {
            //string content = File.ReadAllText("Resources/Pages/404.html");
            return new HttpResponse()
            {
                ReasonPhrase = "NotFound",
                StatusCode = "404",
                ContentAsUTF8 = "<h1>Not Found</h1><small>by SimpleHtppServer</small>"
            };
        }
    }
}
