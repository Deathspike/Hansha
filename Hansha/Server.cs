using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Hansha
{
    public class Server : IRunnable
    {
        private readonly List<IHandler> _handlers;
        private readonly HttpListener _listener;
        private bool _isRunning;

        #region Abstract

        private async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                for (var i = 0; i < _handlers.Count; i++)
                {
                    if (await _handlers[i].HandleAsync(context))
                    {
                        return;
                    }
                }

                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion

        #region Constructor

        public Server(params string[] prefixes)
        {
            _handlers = new List<IHandler>();
            _listener = new HttpListener();

            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
        }

        #endregion

        #region Methods

        public void Add(IHandler handler)
        {
            _handlers.Add(handler);
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _listener.Stop();
            }
        }

        #endregion

        #region Implementation of IRunnable

        public async Task RunAsync()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _listener.Start();

                while (_isRunning)
                {
                    var context = await _listener.GetContextAsync();
                    EventLoop.Run(() => HandleAsync(context));
                }
            }
        }

        #endregion
    }
}