using System.Diagnostics;
using System.Windows;

namespace Battle.reflection {
	public static class M {
		//float sdBox(float2 p, in float2 b) {
		//	var q = abs(p) - b;
		//	return min(max(q.x, q.y), 0.0) + length(max(q, 0.0));
		//}

		public static float hash21(vec2 p) {
			p = fract(p * (234.34, 435.345));
			p += p.dot(p + 34.23);
			return p.x % p.y;
		}


		public static double step(double edge, double x)
			=> x < edge ? 0 : 1;

		public static double smoothstep(double e1, double e2, double x) {
			x = clamp((x-e1)/(e2-e1), 0.0, 1.0);
			//x = (x-e1)/(e2-e1);
			return x * x * (3 - 2 * x);
		}

		public static double clamp(double x, double l, double h)
			=> x < l ? l : (x > h ? h : x);

		public static float min(double a, double b)
			=> (float)System.Math.Min(a, b);

		public static vec3 cos(vec3 f)
			=> new vec3(
				(float)System.Math.Cos(f.x)
				, (float)System.Math.Cos(f.y)
				, (float)System.Math.Cos(f.z)
		);

		public static float dot(vec3 f, vec3 f2) {
			return f.x*f2.x + f.y*f2.y + f.z*f2.z;
		}

		public static float length(vec3 f) {
			return (float)System.Math.Sqrt(f.x * f.x + f.y * f.y + f.z * f.z);
		}

		public static float sqrt(float f)
			=> (float)System.Math.Sqrt(f);
		public static vec3 sqrt(vec3 f)
			=> new vec3(sqrt(f.x), sqrt(f.y), sqrt(f.z));

		public static float exp(double f)
			=> (float)System.Math.Exp(f);

		public static float abs(double f)
			=> (float)System.Math.Abs(f);

		public static vec3 normalize(vec3 f) {
			var l = System.Math.Sqrt(f.x*f.x + f.y*f.y + f.z*f.z);
			return vec3(f.x / l, f.y / l, f.z / l);
		}

		public static vec3 vec3(double u) {
			return new vec3((float)u, (float)u, (float)u);
		}

		public static vec3 vec3(double x, double y, double z) {
			return new vec3((float)x, (float)y, (float)z);
		}
		public static vec3 vec3(vec2 f, double z) {
			return new vec3(f.x, f.y, (float)z);
		}

		public static vec4 vec4(vec3 b, double w) {
			return new vec4(b.x, b.y, b.z, (float)w);
		}
		public static vec4 vec4(double x, double y, double z, double w) {
			return new vec4((float)x, (float)y, (float)z, (float)w);
		}

		public static vec2 fract(vec2 f) => f.fract();
	}
}
