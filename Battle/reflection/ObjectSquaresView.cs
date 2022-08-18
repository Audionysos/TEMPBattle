using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
		private float3 iResolution;
		private void renderBitmap() {
			iResolution = (buffer.size, 0);

			buffer.perPixel(fragCoord => {
				//var uv = fragCoord / buffer.size;
				var uv = (fragCoord - 0.5 * iResolution.xy) / iResolution.y;
				//var uv = (fragCoord-.5*iResolution.xy) / iResolution.y;
				var col = new float3();
				//uv -= .5;
				uv *= 30;

				//if (uv.x > 1.4 && uv.y < 1.45) Debugger.Break();
				var gv = uv.fract() - .5;
				var id = uv.floor();
				var n = hash21(id);

				var w = 0.08;
				if (n < .5) gv.x *= -1;
				var mask = smoothstep(.01, -.01, Abs(gv.x + gv.y) - w);
				//var mask = Abs(gv.x + gv.y);
				col += mask;

				//if (gv.x > .48 || gv.y > .48) col = (1, 0, 0);
				return (col, 1);// (uv.x, uv.y, 0, 1);
			});

			//buffer.perPixel(fragCoord => {
			//	var p = (2.0 * fragCoord - iResolution.xy) / iResolution.y;
			//	// sdf
			//	float d;
			//	if (p.x < 0.0) // correct way to do it
			//	{
			//		var q = p * 6.0 + new float3(5.0f, 0.0f);
			//		var r = opRepLim(q, 2.0, new float2(1.0f, 2.0f));
			//		d = sdBox(r, new float2(0.4f, 0.2f)) - 0.1;
			//	} else         // incorrect way to do it
			//	  {
			//		var q = p * 6.0 - new float2(5.0f, 0.0f);
			//		var r = opRep(q, 2.0);
			//		d = sdBox(r, new float2(0.4, 0.2)) - 0.1;
			//		d = opIntersection(d, sdBox(q, new float2(2.5, 4.5)));
			//	}

			//	// colorize
			//	var col = new float3(1.0) - Sign(d) * new float3(0.1, 0.4, 0.7);
			//	col *= 1.0 - exp(-2.0 * abs(d));
			//	col *= 0.8 + 0.2 * Cos(40.0 * d);
			//	col = mix(col, new float3(1.0), 1.0 - smoothstep(0.0, 0.05, abs(d)));

			//	col *= smoothstep(0.005, 0.010, abs(p.x));

			//	return (col, 1.0);
			//});


			buffer.writeTo(bm);
		}

		//float sdBox(float2 p, in float2 b) {
		//	var q = abs(p) - b;
		//	return min(max(q.x, q.y), 0.0) + length(max(q, 0.0));
		//}

		private float hash21(float2 p) {
			p = fract(p * (234.34, 435.345));
			p += p.dot(p + 34.23);
			return p.x % p.y;
		}

		public void xxx(out int x, in int y) {
			x = 0;
		}

		private (double x, double y) getDPI() {
			var src = PresentationSource.FromVisual(this);
			if (src != null) return (
				96.0 * src.CompositionTarget.TransformToDevice.M11,
				96.0 * src.CompositionTarget.TransformToDevice.M22);
			return (96, 96);
		}

	}

	public class BitmapData<T> where T : struct {
		public (int x, int y) size { get; private set; }
		public T[] data { get; private set; }

		public BitmapData(int widht, int height) {
			size = (widht, height);
			data = new T[size.x * size.y];
		}

		public void writeTo(WriteableBitmap bm) {
			var s = size.x * Marshal.SizeOf<T>();
			if (s % 4 != 0) Debugger.Break();
			bm.Lock();
			var wg = bm.Width == size.x;
			var hg = bm.Height == size.y;
			bm.WritePixels(
				new Int32Rect(0, 0, size.x, size.y),
				data, s, 0, 0
			);
			bm.Unlock();
		}


		public void perPixel(Func<float2,float4> shdr)
		{
			for (int y = 0; y < size.y; y++) {
				for (int x = 0; x <  size.x; x++) {
					//var v = (T)(object)0xFFFF_0000_0000_FFFF;
					//var v = (T)(object)(ulong)0x0000_FFFF_0000_FFFF;
					var v2 = floatToGeneric(shdr((x+500, y+500)));
					//if (!v.Equals(v2)) y = y;
					this[x, y] = v2;
				}
			}
		}

		private T floatToGeneric(float4 c) {
			//var f = ((ulong)(c.a * 0xFFFF) & 0xFFFF) |
			//		(((ulong)(c.b * 0xFFFF) & 0xFFFF) << 16) |
			//		(((ulong)(c.g * 0xFFFF) & 0xFFFF) << 32) |
			//		(((ulong)(c.r * 0xFFFF) & 0xFFFF) << 48);
			//return (T)(object)f;
			//var f = ((uint)(c.b * 255) & 0xFF) |
			//		(((uint)(c.g * 255) & 0xFF) << 8) |
			//		(((uint)(c.r * 255) & 0xFF) << 16) |
			//		(((uint)(c.a * 255) & 0xFF) << 24);
			//return (T)(object)f;
			var f = ((uint)(Max(c.b, 0) * 255) & 0xFF) |
					(((uint)(Max(c.g, 0) * 255) & 0xFF) << 8) |
					(((uint)(Max(c.r, 0) * 255) & 0xFF) << 16) |
					(((uint)(Max(c.a, 0) * 255) & 0xFF) << 24);
			return (T)(object)f;
		}

		public T this[int i] { get => data[i]; set => data[i] = value; }
		public T this[int x, int y] {
			get => data[y * size.x + x];
			set => data[y * size.x + x] = value;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct float4 {
		#region Fields
		[FieldOffset(0)]
		public fixed float data[4];
		[FieldOffset(0)] public float x;
		[FieldOffset(0)] public float r;

		[FieldOffset(4)] public float y;
		[FieldOffset(4)] public float g;

		[FieldOffset(8)] public float z;
		[FieldOffset(8)] public float b;

		[FieldOffset(12)] public float w;
		[FieldOffset(12)] public float a;
		#endregion

		#region Constructors
		public float4(float x = 0, float y = 0, float z = 0, float w = 0) {
			//data = new float[4];
			this.x = r = x;
			this.y = g = y;
			this.z = b = z;
			this.w = a = w;
		}

		public float4(float3 f3, float w) {
			this.x = r = f3.x;
			this.y = g = f3.y;
			this.z = b = f3.z;
			this.w = a = w;
		}
		#endregion

		public override string ToString() {
			return $@"V4({x}, {y}, {z}, {w})";
		}

		public static implicit operator float4((int x, int y) t) => new float4(t.x, t.y);
		public static implicit operator float4((int x, int y, int z, int w) t)
			=> new float4(t.x, t.y, t.z, t.w);

		public static implicit operator float4((float x, float y, float z, float w) t)
			=> new float4(t.x, t.y, t.z, t.w);
		public static implicit operator float4((float3 f3, float w) t)
			=> new float4(t.f3, t.w);
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct float3 {
		#region Fields
		[FieldOffset(0)]
		public fixed float data[4];
		[FieldOffset(0)] public float x;
		[FieldOffset(0)] public float r;

		[FieldOffset(4)] public float y;
		[FieldOffset(4)] public float g;

		[FieldOffset(8)] public float z;
		[FieldOffset(8)] public float b;
		#endregion

		public float2 xy {
			get => (r, g);
			set { r = value.r; g = value.g; }
		}
		public float2 rg {
			get => (r, g);
			set { r = value.r; g = value.g; }
		}

		public float3(float x = 0, float y = 0, float z = 0) {
			this.x = r = x;
			this.y = g = y;
			this.z = b = z;
		}

		public float3(float2 f2, float z = 0) {
			this.x = r = f2.x;
			this.y = g = f2.y;
			this.z = b = z;
		}

		public override string ToString() {
			return $@"V4({x}, {y}, {z})";
		}

		public static float3 operator +(float3 f1, float f2) =>
			new float3(f1.x + f2, f1.y + f2, f1.z + f2);
		public static float3 operator +(float3 f1, double f2) =>
			new float3(f1.x + (float)f2, f1.y + (float)f2, f1.z + (float)f2);

		public static implicit operator float3((int x, int y) t) => new float3(t.x, t.y);
		public static implicit operator float3((int x, int y, int z, int w) t)
			=> new float3(t.x, t.y, t.z);

		public static implicit operator float3((float x, float y, float z, float w) t)
			=> new float3(t.x, t.y, t.z);
		public static implicit operator float3((float x, float y, float z) t)
			=> new float3(t.x, t.y, t.z);

		public static implicit operator float3((float2 f2, float z) t)
			=> new float3(t.f2, t.z);
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct float2 {
		[FieldOffset(0)]
		public fixed float data[2];
		[FieldOffset(0)] public float x;
		[FieldOffset(0)] public float r;

		[FieldOffset(4)] public float y;
		[FieldOffset(4)] public float g;

		public float2 rg {
			get => (r, g);
			set { r = value.r; g = value.g; }
		}

		public float2(float x = 0, float y = 0) {
			//data = new float[4];
			this.x = r = x;
			this.y = g = y;
		}

		public float dot(float2 o) => x * o.x + y * o.y;

		public override string ToString() {
			return $@"V2({x}, {y})";
		}

		public float2 fract() => new float2(x % 1, y % 1);

		internal float2 floor() => new float2((int)x, (int)y);

		public static float2 operator /(float2 f1, float f2) =>
			new float2(f1.x / f2, f1.y / f2);

		public static float2 operator /(float2 f1, float2 f2) =>
			new float2(f1.x / f2.x, f1.y / f2.y);
		public static float2 operator *(float2 f1, float2 f2) =>
			new float2(f1.x * f2.x, f1.y * f2.y);

		public static float2 operator *(float2 f1, double f2) =>
			new float2(f1.x * (float)f2, f1.y * (float)f2);
		public static float2 operator *(double f2, float2 f1) =>
			new float2(f1.x * (float)f2, f1.y * (float)f2);

		public static float2 operator -(float2 f1, float f2) =>
			new float2(f1.x - f2, f1.y- f2);
		public static float2 operator -(float2 f1, double f2) =>
			new float2(f1.x - (float)f2, f1.y - (float)f2);
		public static float2 operator -(float2 f1, float2 f2) =>
			new float2(f1.x - f2.x, f1.y - f2.x);

		public static float2 operator +(float2 f1, float f2) =>
			new float2(f1.x + f2, f1.y + f2);
		public static float2 operator +(float2 f1, double f2) =>
			new float2(f1.x + (float)f2, f1.y + (float)f2);

		public static implicit operator float2((int x, int y) t) => new float2(t.x, t.y);
		public static implicit operator float2((int x, int y, int z, int w) t)
			=> new float2(t.x, t.y);

		public static implicit operator float2((float x, float y, float z, float w) t)
			=> new float2(t.x, t.y);
		public static implicit operator float2((float x, float y) t)
			=> new float2(t.x, t.y);
		public static implicit operator float2((double x, double y) t)
			=> new float2((float)t.x, (float)t.y);

	}

	public static class M {

		public static double smoothstep(double e1, double e2, double x) {
			x = clamp((x-e1)/(e2-e1), 0.0, 1.0);
			//x = (x-e1)/(e2-e1);
			return x * x * (3 - 2 * x);
		}

		public static double clamp(double x, double l, double h)
			=> x < l ? l : (x > h ? h : x);

		public static float2 fract(float2 f) => f.fract();
	}
}
