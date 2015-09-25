using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hansha.TestProtocol
{
    public partial class TestWindow : Form
    {
        public TestWindow()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                var bitmap = new Bitmap(1920, 1080, PixelFormat.Format32bppRgb);
                pictureBox1.Image = bitmap;
                Task.Run(() => TestProtocolCore.Run(this, bitmap));
            }
        }
    }
}