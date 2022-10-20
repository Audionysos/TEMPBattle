using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Battle.reflection.shaders;
using static System.Math;
using static Battle.reflection.M;

namespace Battle.reflection {

	public class ObjectSquaresView : FrameworkElement {
		private WriteableBitmap bm;

		public ObjectSquaresView() {
			Loaded += onLoaded;
		}

		private void onLoaded(object sender, RoutedEventArgs e) {
			prepareData(((int)ActualWidth, (int)ActualHeight));
			renderBitmap();
			InvalidateVisual();
		}

		Stopwatch sw;
		protected override void OnRenderSizeChanged(SizeChangedInfo si) {
			prepareData(((int)Ceiling(si.NewSize.Width), (int)Ceiling(si.NewSize.Height)));
			//renderBitmap();
			base.OnRenderSizeChanged(si);
		}

		private void prepareData((int x, int y) s) {
			var dpi = getDPI();
			bm = new WriteableBitmap(
				s.x, s.y,
				dpi.x, dpi.y,
				PixelFormats.Bgra32,
				null);
			buffer = new BitmapData<uint>(s.x, s.y);
		}

		protected override void OnRender(DrawingContext ctx) {
			if (bm == null) return;
			sw = Stopwatch.StartNew();
			renderBitmap();
			ctx.DrawImage(bm, new Rect(0,0, bm.Width, bm.Height));
			sw.Stop();
			Debug.WriteLine($@"{sw.ElapsedMilliseconds.ToString("##.###")}ms");
		}

		private BitmapData<uint> buffer;

		private Shader shd = new Sphere();

		private void renderBitmap() {
			shd.iResolution = (buffer.size, 0);
			buffer.perPixel(shd.main);
			buffer.writeTo(bm);
		}

		private (double x, double y) getDPI() {
			var src = PresentationSource.FromVisual(this);
			if (src != null) return (
				96.0 * src.CompositionTarget.TransformToDevice.M11,
				96.0 * src.CompositionTarget.TransformToDevice.M22);
			return (96, 96);
		}

	}
}
