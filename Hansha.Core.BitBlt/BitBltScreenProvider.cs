namespace Hansha.Core.BitBlt
{
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