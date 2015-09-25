using System.ComponentModel.Composition;

namespace Hansha.Core.BitBlt
{
    [Export(typeof(IScreenProvider))]
    public class BitBltScreenProvider : IScreenProvider
    {
        #region Implementation of IScreenProvider

        public IScreen GetScreen()
        {
            return new BitBltScreen(Win32.GetDesktopWindow());
        }

        #endregion
    }
}