using System.Runtime.InteropServices;

namespace Battle.reflection {
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct vec2 {
		[FieldOffset(0)]
		public fixed float data[2];
		[FieldOffset(0)] public float x;
		[FieldOffset(0)] public float r;

		[FieldOffset(4)] public float y;
		[FieldOffset(4)] public float g;

		public vec2 rg {
			get => (r, g);
			set { r = value.r; g = value.g; }
		}

		public vec2 xy => new vec2(x, y);

		public vec2(float x = 0, float y = 0) {
			//data = new float[4];
			this.x = r = x;
			this.y = g = y;
		}

		public float dot(vec2 o) => x * o.x + y * o.y;

		public override string ToString() {
			return $@"V2({x}, {y})";
		}

		public vec2 fract() => new vec2(x % 1, y % 1);

		internal vec2 floor() => new vec2((int)x, (int)y);

		public static vec2 operator /(vec2 f1, float f2) =>
			new vec2(f1.x / f2, f1.y / f2);

		public static vec2 operator /(vec2 f1, vec2 f2) =>
			new vec2(f1.x / f2.x, f1.y / f2.y);
		public static vec2 operator *(vec2 f1, vec2 f2) =>
			new vec2(f1.x * f2.x, f1.y * f2.y);

		public static vec2 operator *(vec2 f1, double f2) =>
			new vec2(f1.x * (float)f2, f1.y * (float)f2);
		public static vec2 operator *(double f2, vec2 f1) =>
			new vec2(f1.x * (float)f2, f1.y * (float)f2);

		public static vec2 operator -(vec2 f1, float f2) =>
			new vec2(f1.x - f2, f1.y- f2);
		public static vec2 operator -(vec2 f1, double f2) =>
			new vec2(f1.x - (float)f2, f1.y - (float)f2);
		public static vec2 operator -(vec2 f1, vec2 f2) =>
			new vec2(f1.x - f2.x, f1.y - f2.y);

		public static vec2 operator +(vec2 f1, float f2) =>
			new vec2(f1.x + f2, f1.y + f2);
		public static vec2 operator +(vec2 f1, double f2) =>
			new vec2(f1.x + (float)f2, f1.y + (float)f2);

		public static implicit operator vec2((int x, int y) t) => new vec2(t.x, t.y);
		public static implicit operator vec2((int x, int y, int z, int w) t)
			=> new vec2(t.x, t.y);

		public static implicit operator vec2((float x, float y, float z, float w) t)
			=> new vec2(t.x, t.y);
		public static implicit operator vec2((float x, float y) t)
			=> new vec2(t.x, t.y);
		public static implicit operator vec2((double x, double y) t)
			=> new vec2((float)t.x, (float)t.y);

	}
}
