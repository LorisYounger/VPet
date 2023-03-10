using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VPet_Simulator.Core.New
{
    /// <summary>
    /// Interaction logic for AnimationCanvas.xaml
    /// </summary>
    public partial class AnimationCanvas : Image, IGraphNew
    {
        public AnimationCanvas()
        {
            InitializeComponent();
        }

        public void Order(Stream stream)
        {
            Dispatcher.Invoke(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                stream.Seek(0, SeekOrigin.Begin);
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                Source = bitmap;
            });
        }

        void IGraphNew.Clear()
        {
            Dispatcher.Invoke(() => Source = null);
        }
    }
}