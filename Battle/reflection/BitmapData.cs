﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using static System.Math;

namespace Battle.reflection {
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

		public void perPixel(Func<vec2, vec4> shdr){
			for (int y = 0; y < size.y; y++) {
				for (int x = 0; x <  size.x; x++) {
					//var v = (T)(object)0xFFFF_0000_0000_FFFF;
					//var v = (T)(object)(ulong)0x0000_FFFF_0000_FFFF;
					var v2 = floatToGeneric(shdr((x, y)));
					//if (!v.Equals(v2)) y = y;
					this[x, size.y - y-1] = v2;
				}
			}
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
}
