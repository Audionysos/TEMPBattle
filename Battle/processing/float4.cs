using System.Runtime.InteropServices;

namespace adns.processing {
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct vec4 {
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

		public vec3 xyz => new vec3(x, y, z);
		#endregion

		#region Constructors
		public vec4(float x = 0, float y = 0, float z = 0, float w = 0) {
			//data = new float[4];
			this.x = r = x;
			this.y = g = y;
			this.z = b = z;
			this.w = a = w;
		}

		public vec4(vec3 f3, float w) {
			x = r = f3.x;
			y = g = f3.y;
			z = b = f3.z;
			this.w = a = w;
		}
		#endregion

		public override string ToString() {
			return $@"V4({x}, {y}, {z}, {w})";
		}

		public static implicit operator vec4((int x, int y) t) => new vec4(t.x, t.y);
		public static implicit operator vec4((int x, int y, int z, int w) t)
			=> new vec4(t.x, t.y, t.z, t.w);

		public static implicit operator vec4((float x, float y, float z, float w) t)
			=> new vec4(t.x, t.y, t.z, t.w);
		public static implicit operator vec4((vec3 f3, float w) t)
			=> new vec4(t.f3, t.w);
	}
}
