using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace Hansha.Core.DesktopDuplication
{
    // TODO: Optimize `ModifiedRegions` and `MovedRegions` array allocation.
    public class DesktopDuplicationScreen : IScreen
    {
        private readonly Device _device;
        private readonly RawRectangle _desktopBounds;
        private readonly ScreenFrame _frame;
        private readonly OutputDuplication _outputDuplication;
        private readonly Texture2DDescription _texture2DDescription;
        private Texture2D _desktopImageTexture;
        private OutputDuplicateFrameInformation _frameInfo;
        private byte[] _newPixels;
        private byte[] _previousPixels;

        #region Abstract

        private bool AcquireNextFrame(int timeoutInMilliseconds)
        {
            Resource resource = null;

            try
            {
                _outputDuplication.AcquireNextFrame(timeoutInMilliseconds, out _frameInfo, out resource);

                if (resource == null)
                {
                    return false;
                }
            }
            catch (SharpDXException e)
            {
                if (e.ResultCode.Code == ResultCode.WaitTimeout.Result.Code)
                {
                    return false;
                }

                if (e.ResultCode.Failure)
                {
                    throw new ScreenException("Unable to access next frame.");
                }
            }

            using (resource)
            {
                if (_desktopImageTexture == null)
                {
                    _desktopImageTexture = new Texture2D(_device, _texture2DDescription);
                }

                using (var newTexture = resource.QueryInterface<Texture2D>())
                {
                    _device.ImmediateContext.CopyResource(newTexture, _desktopImageTexture);
                    return true;
                }
            }
        }

        private void ProcessBoundaries()
        {
            _frame.Boundaries.Bottom = _desktopBounds.Bottom;
            _frame.Boundaries.Left = _desktopBounds.Left;
            _frame.Boundaries.Right = _desktopBounds.Right;
            _frame.Boundaries.Top = _desktopBounds.Top;
        }

        private void ProcessModifiedRegions()
        {
            var numberOfBytes = 0;
            var rectangles = new RawRectangle[_frameInfo.TotalMetadataBufferSize];

            if (_frameInfo.TotalMetadataBufferSize > 0)
            {
                _outputDuplication.GetFrameDirtyRects(rectangles.Length, rectangles, out numberOfBytes);
            }

            _frame.ModifiedRegions = new ScreenFrameRectangle[numberOfBytes / Marshal.SizeOf(typeof(RawRectangle))];

            for (var i = 0; i < _frame.ModifiedRegions.Length; i++)
            {
                _frame.ModifiedRegions[i].Bottom = rectangles[i].Bottom;
                _frame.ModifiedRegions[i].Left = rectangles[i].Left;
                _frame.ModifiedRegions[i].Right = rectangles[i].Right;
                _frame.ModifiedRegions[i].Top = rectangles[i].Top;
            }
        }

        private void ProcessMoveRegions()
        {
            var numberOfBytes = 0;
            var rectangles = new OutputDuplicateMoveRectangle[_frameInfo.TotalMetadataBufferSize];

            if (_frameInfo.TotalMetadataBufferSize > 0)
            {
                _outputDuplication.GetFrameMoveRects(rectangles.Length, rectangles, out numberOfBytes);
            }

            _frame.MovedRegions = new ScreenFrameRegion[numberOfBytes / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle))];

            for (var i = 0; i < _frame.MovedRegions.Length; i++)
            {
                _frame.MovedRegions[i].Destination.Bottom = rectangles[i].DestinationRect.Bottom;
                _frame.MovedRegions[i].Destination.Left = rectangles[i].DestinationRect.Left;
                _frame.MovedRegions[i].Destination.Right = rectangles[i].DestinationRect.Right;
                _frame.MovedRegions[i].Destination.Top = rectangles[i].DestinationRect.Top;
                _frame.MovedRegions[i].X = rectangles[i].SourcePoint.X;
                _frame.MovedRegions[i].Y = rectangles[i].SourcePoint.Y;
            }
        }

        private void ProcessPixels()
        {
            try
            {
                var mapSubresource = _device.ImmediateContext.MapSubresource(_desktopImageTexture, 0, MapMode.Read, MapFlags.None);

                if (_newPixels == null || _previousPixels == null)
                {
                    var numberOfBytes = _desktopBounds.Right * _desktopBounds.Bottom * 4;
                    _newPixels = new byte[numberOfBytes];
                    _previousPixels = new byte[numberOfBytes];
                }
                else
                {
                    var swapPixels = _previousPixels;
                    _previousPixels = _newPixels;
                    _newPixels = swapPixels;
                }

                Marshal.Copy(mapSubresource.DataPointer, _newPixels, 0, _newPixels.Length);
            }
            finally
            {
                _device.ImmediateContext.UnmapSubresource(_desktopImageTexture, 0);
                _frame.NewPixels = _newPixels;
                _frame.PreviousPixels = _previousPixels;
            }
        }

        private void ReleaseFrame()
        {
            try
            {
                _outputDuplication.ReleaseFrame();
            }
            catch (SharpDXException e)
            {
                if (e.ResultCode.Failure)
                {
                    throw new ScreenException("Failed to release frame.");
                }
            }
        }

        #endregion

        #region Constructor

        public DesktopDuplicationScreen(int adapterIndex, int outputIndex)
        {
            using (var adapter = InitializeAdapter(adapterIndex))
            {
                _device = new Device(adapter);

                using (var output = InitializeOutput(adapter, outputIndex))
                {
                    _desktopBounds = output.Description.DesktopBounds;
                    _frame = new ScreenFrame();
                    _outputDuplication = InitializeOutputDuplication(output);
                    _texture2DDescription = InitializeTexture2DDescription(_desktopBounds);
                }
            }
        }

        #endregion

        #region Initializers

        private Adapter1 InitializeAdapter(int adapterIndex)
        {
            try
            {
                using (var factory = new Factory1())
                {
                    return factory.GetAdapter1(adapterIndex);
                }
            }
            catch (SharpDXException)
            {
                throw new ScreenException("Unable to initialize the adapter.");
            }
        }

        private Output InitializeOutput(Adapter1 adapter, int outputIndex)
        {
            try
            {
                return adapter.GetOutput(outputIndex);
            }
            catch (SharpDXException)
            {
                throw new ScreenException("Unable to initialize the output device.");
            }
        }

        private OutputDuplication InitializeOutputDuplication(Output output)
        {
            try
            {
                using (var output1 = output.QueryInterface<Output1>())
                {
                    return output1.DuplicateOutput(_device);
                }
            }
            catch (SharpDXException)
            {
                throw new ScreenException("Unable to initialize the Desktop Duplication API.");
            }
        }

        private Texture2DDescription InitializeTexture2DDescription(RawRectangle desktopBounds)
        {
            return new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = Format.B8G8R8A8_UNorm,
                Height = desktopBounds.Bottom,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging,
                Width = desktopBounds.Right
            };
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            _outputDuplication.Dispose();
            _device.Dispose();
        }

        #endregion

        #region Implementation of IScreen

        public ScreenFrame GetFrame(int timeoutInMilliseconds)
        {
            if (!AcquireNextFrame(timeoutInMilliseconds))
            {
                return null;
            }

            try
            {
                ProcessBoundaries();
                ProcessModifiedRegions();
                ProcessMoveRegions();
                ProcessPixels();
                return _frame;
            }
            finally
            {
                ReleaseFrame();
            }
        }

        #endregion
    }
}