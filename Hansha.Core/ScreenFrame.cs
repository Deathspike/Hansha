namespace Hansha.Core
{
    public struct ScreenFrame
    {
        public ScreenFrameRectangle Boundaries;
        public ScreenFrameRectangle[] ModifiedRegions;
        public ScreenFrameRegion[] MovedRegions;
        public byte[] NewPixels;
        public byte[] PreviousPixels;
    }
}