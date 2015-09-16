using System;
using System.Threading.Tasks;

namespace Hansha
{
    public interface IRunnable : IDisposable
    {
        Task RunAsync();
    }
}