using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Hansha
{
    public class StaticHandler : IHandler
    {
        private readonly string _rootPath;

        #region Abstract

        public string GetContentType(string extension)
        {
            switch ((extension ?? string.Empty).ToLower())
            {
                case ".css":
                    return "text/css";
                case ".html":
                    return "text/html";
                case ".js":
                    return "application/javascript";
                default:
                    return "text/plain";
            }
        }

        #endregion

        #region Constructor

        public StaticHandler(string rootPath)
        {
            _rootPath = rootPath;
        }

        #endregion

        #region Implementation of IHandler

        public async Task<bool> HandleAsync(HttpListenerContext context)
        {
            var rawUrl = context.Request.RawUrl + (context.Request.RawUrl.EndsWith("/") ? "index.html" : string.Empty);

            try
            {
                using (var stream = File.OpenRead(Path.Combine(_rootPath, rawUrl.Substring(1))))
                {
                    context.Response.ContentType = GetContentType(Path.GetExtension(rawUrl));
                    await stream.CopyToAsync(context.Response.OutputStream);
                    context.Response.Close();
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        #endregion
    }
}