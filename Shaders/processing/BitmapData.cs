using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static System.Math;

namespace adns.processing {
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

		public void perPixel(Func<vec2, vec4> shdr) {
			for (int y = 0; y < size.y; y++) {
				for (int x = 0; x < size.x; x++) {
					//var v = (T)(object)0xFFFF_0000_0000_FFFF;
					//var v = (T)(object)(ulong)0x0000_FFFF_0000_FFFF;
					var v2 = floatToGeneric(shdr((x, y)));
					//if (!v.Equals(v2)) y = y;
					this[x, size.y - y - 1] = v2;
				}
			}
		}

		public void perPixel(Func<vec2, vec4> shdr, int chunks = 16) {
			var chs = (double)size.x * size.y / chunks;
			var s = (int)Ceiling(Sqrt(Ceiling(chs)));
			var ts = new List<Task>();
			for (int ly = 0; ly < size.y;) {
				for (int lx = 0; lx < size.x;) {
					var sx = lx; var sy = ly;
					var t = Task.Run(() => {
						var mx = Min(size.x, sx + s);
						var my = Min(size.y, sy + s);
						for (int x = sx; x < mx; x++) {
							for (int y = sy; y < my; y++) {
								var v2 = floatToGeneric(shdr((x, y)));
								this[x, size.y - y - 1] = v2;
							}
						}
					});

					ts.Add(t);
					lx += s;
				}
				ly += s;
			}
			Task.WaitAll(ts.ToArray());
		}

		private T floatToGeneric(vec4 c) {
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
			var f = (uint)(Max(c.b, 0) * 255) & 0xFF |
					((uint)(Max(c.g, 0) * 255) & 0xFF) << 8 |
					((uint)(Max(c.r, 0) * 255) & 0xFF) << 16 |
					((uint)(Max(c.a, 0) * 255) & 0xFF) << 24;
			return (T)(object)f;
		}

		public T this[int i] { get => data[i]; set => data[i] = value; }
		public T this[int x, int y] {
			get => data[y * size.x + x];
			set => data[y * size.x + x] = value;
		}
	}
}
