using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Hansha.Core;
using Hansha.Core.DesktopDuplication;

namespace Hansha.Screenshot
{
    public class Program
    {
        private static void CopyToImage(Bitmap image, ScreenFrame screenFrame)
        {
            var imageBoundaries = new Rectangle(0, 0, screenFrame.Boundaries.Right, screenFrame.Boundaries.Bottom);
            var imageData = image.LockBits(imageBoundaries, ImageLockMode.WriteOnly, image.PixelFormat);
            Marshal.Copy(screenFrame.NewPixels, 0, imageData.Scan0, screenFrame.NewPixels.Length);
            image.UnlockBits(imageData);
        }

        private static Bitmap CreateImage(ScreenFrame screenFrame)
        {
            return new Bitmap(screenFrame.Boundaries.Right, screenFrame.Boundaries.Bottom, PixelFormat.Format32bppRgb);
        }

        public static void Main()
        {
            var screenProvider = new DesktopDuplicationScreenProvider();

            using (var screen = screenProvider.GetScreen())
            {
                var screenFrame = screen.GetFrame(Timeout.Infinite);

                using (var image = CreateImage(screenFrame))
                {
                    CopyToImage(image, screenFrame);
                    image.Save("ExampleScreenshot.png");
                }
            }
        }
    }
}