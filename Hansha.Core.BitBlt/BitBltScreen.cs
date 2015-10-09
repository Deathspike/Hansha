using System;
using System.Runtime.InteropServices;

namespace Hansha.Core.BitBlt
{
    // TODO: Optimize `ModifiedRegions` and `MovedRegions` array allocation.
    public class BitBltScreen : IScreen
    {
        private readonly ScreenFrame _frame;
        private readonly IntPtr _windowHandle;
        private IntPtr _handleBitmap;
        private IntPtr _handleDeviceContext;
        private IntPtr _handleMemoryDeviceContext;
        private byte[] _newPixels;
        private byte[] _previousPixels;
        private BitmapInfoHeader _structBitmapInfoHeader;
        private Rect _structRect;

        #region Abstract

        private void UpdateBuffer()
        {
            var sizeInBytes = _structRect.Right * _structRect.Bottom * 4;
            _previousPixels = new byte[sizeInBytes];
            _newPixels = new byte[sizeInBytes];
        }

        private void UpdateContext()
        {
            _handleMemoryDeviceContext = Win32.CreateCompatibleDC(_handleDeviceContext);
            _handleDeviceContext = Win32.GetDC(_windowHandle);
            _handleBitmap = Win32.CreateCompatibleBitmap(_handleDeviceContext, _structRect.Right, _structRect.Bottom);
            Win32.SelectObject(_handleMemoryDeviceContext, _handleBitmap);
        }

        private void UpdateRect()
        {
            Win32.GetClientRect(_windowHandle, out _structRect);
            _structBitmapInfoHeader.BiBitCount = 32;
            _structBitmapInfoHeader.BiPlanes = 1;
            _structBitmapInfoHeader.BiHeight = -_structRect.Bottom;
            _structBitmapInfoHeader.BiSize = (uint) Marshal.SizeOf(typeof (BitmapInfoHeader));
            _structBitmapInfoHeader.BiWidth = _structRect.Right;
        }

        #endregion

        #region Constructor

        public BitBltScreen(IntPtr windowHandle)
        {
            _frame = new ScreenFrame();
            _windowHandle = windowHandle;
            UpdateRect();
            UpdateContext();
            UpdateBuffer();
        }

        #endregion
        
        #region Implementation of IDisposable

        public void Dispose()
        {
            if (_newPixels == null || _previousPixels == null) return;
            Win32.ReleaseDC(_windowHandle, _handleBitmap);
            Win32.ReleaseDC(_windowHandle, _handleDeviceContext);
            Win32.DeleteObject(_handleBitmap);
            Win32.DeleteObject(_handleMemoryDeviceContext);
        }

        #endregion

        #region Implementation of IScreen

        public ScreenFrame GetFrame(int timeoutInMilliseconds)
        {
            var swapPixels = _previousPixels;
            _previousPixels = _newPixels;
            _newPixels = swapPixels;

            Win32.BitBlt(_handleMemoryDeviceContext, 0, 0, _structRect.Right, _structRect.Bottom, _handleDeviceContext, 0, 0, 0xCC0020);
            Win32.GetDIBits(_handleMemoryDeviceContext, _handleBitmap, 0, _structRect.Bottom, _newPixels, ref _structBitmapInfoHeader, 0);

            _frame.Boundaries.Bottom = _structRect.Bottom;
            _frame.Boundaries.Right = _structRect.Right;

            _frame.ModifiedRegions = new ScreenFrameRectangle[1];
            _frame.ModifiedRegions[0].Bottom = _structRect.Bottom;
            _frame.ModifiedRegions[0].Right = _structRect.Right;
            _frame.MovedRegions = new ScreenFrameRegion[0];

            _frame.NewPixels = _newPixels;
            _frame.PreviousPixels = _previousPixels;
            return _frame;
        }

        #endregion
    }
}