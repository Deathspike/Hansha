using System;

namespace Hansha.Core
{
    public interface IScreen : IDisposable
    {
        ScreenFrame GetFrame(int timeoutInMilliseconds);
    }
}