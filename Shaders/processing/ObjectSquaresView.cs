using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using adns.processing.shaders;
using static System.Math;

namespace adns.processing {

	public class ObjectSquaresView : FrameworkElement {
		private WriteableBitmap? bm;
		private BitmapData<uint>? buffer;
		private Shader shd = new Sphere();

		protected override Size MeasureOverride(Size availableSize) {
			prepareData(((int)Ceiling(availableSize.Width), (int)Ceiling(availableSize.Height)));
			return base.MeasureOverride(availableSize);
		}

		private void prepareData((int x, int y) s) {
			//var dpi = getDPI();
			bm = new WriteableBitmap(
				s.x, s.y,
				96, 96, //dpi.x, dpi.y,
				PixelFormats.Bgra32,
				null);
			buffer = new BitmapData<uint>(s.x, s.y);
		}

		protected override void OnRender(DrawingContext ctx) {
			if (bm == null) return;
			var sw = Stopwatch.StartNew();
			renderBitmap();
			ctx.DrawImage(bm, new Rect(0,0, bm.Width, bm.Height));
			sw.Stop();
			//Debug.WriteLine($@"{sw.ElapsedMilliseconds.ToString("##.###")}ms");
		}

		private void renderBitmap() {
			shd.iResolution = (buffer.size, 0);
			//buffer.perPixel(shd.main);
			buffer.perPixel(shd.main, 16);
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
