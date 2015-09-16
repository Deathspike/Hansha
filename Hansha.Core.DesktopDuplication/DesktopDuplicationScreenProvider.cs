using System.Threading;

namespace Hansha.Core.DesktopDuplication
{
    // TODO: Figure out why Desktop Duplication API fails to capture the screen without an initial delay.
    public class DesktopDuplicationScreenProvider : IScreenProvider
    {
        #region Implementation of IScreenProvider

        public IScreen GetScreen()
        {
            var screen = new DesktopDuplicationScreen(0, 0);
            Thread.Sleep(250);
            return screen;
        }

        #endregion
    }
}