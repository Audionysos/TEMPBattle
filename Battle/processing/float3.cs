using System.Runtime.InteropServices;

namespace adns.processing {
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct vec3 {
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

		public vec2 xy {
			get => (r, g);
			set { r = value.r; g = value.g; }
		}
		public vec2 rg {
			get => (r, g);
			set { r = value.r; g = value.g; }
		}

		public vec3(float x = 0, float y = 0, float z = 0) {
			this.x = r = x;
			this.y = g = y;
			this.z = b = z;
		}

		public vec3(vec2 f2, float z = 0) {
			x = r = f2.x;
			y = g = f2.y;
			this.z = b = z;
		}

		public override string ToString() {
			return $@"V4({x}, {y}, {z})";
		}

		public static vec3 operator +(vec3 f1, float f2) =>
			new vec3(f1.x + f2, f1.y + f2, f1.z + f2);
		public static vec3 operator +(float f2, vec3 f1) =>
			new vec3(f1.x + f2, f1.y + f2, f1.z + f2);
		public static vec3 operator +(vec3 f1, double f2) =>
			new vec3(f1.x + (float)f2, f1.y + (float)f2, f1.z + (float)f2);
		public static vec3 operator +(vec3 f1, vec3 f2) =>
			new vec3(f1.x + f2.x, f1.y + f2.y, f1.z + f2.z);

		public static vec3 operator -(vec3 f1, float f2) =>
			new vec3(f1.x + f2, f1.y + f2, f1.z + f2);
		public static vec3 operator -(vec3 f1, double f2) =>
			new vec3(f1.x + (float)f2, f1.y + (float)f2, f1.z + (float)f2);
		public static vec3 operator -(vec3 f1, vec3 f2) =>
			new vec3(f1.x - f2.x, f1.y - f2.y, f1.z - f2.z);

		public static vec3 operator *(vec3 f1, vec3 f2)
			=> new vec3(f1.x * f2.x, f1.y * f2.y, f1.z * f2.z);
		public static vec3 operator *(float f1, vec3 f2)
			=> new vec3(f1 * f2.x, f1 * f2.y, f1 * f2.z);

		public static vec3 operator *(vec3 f2, double f1)
			=> new vec3((float)f1 * f2.x, (float)f1 * f2.y, (float)f1 * f2.z);

		public static implicit operator vec3((int x, int y) t) => new vec3(t.x, t.y);
		public static implicit operator vec3((int x, int y, int z, int w) t)
			=> new vec3(t.x, t.y, t.z);

		public static implicit operator vec3((float x, float y, float z, float w) t)
			=> new vec3(t.x, t.y, t.z);
		public static implicit operator vec3((float x, float y, float z) t)
			=> new vec3(t.x, t.y, t.z);

		public static implicit operator vec3((vec2 f2, float z) t)
			=> new vec3(t.f2, t.z);
	}
}
