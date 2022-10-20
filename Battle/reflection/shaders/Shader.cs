using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battle.reflection.shaders {
	public abstract class Shader {
		public vec3 iResolution;
		public float iTime = 1;
		public vec3 iMouse = new vec3(1, 1, 1);

		protected abstract void mainImage(out vec4 fragColor, in vec2 fragCoord);

		public vec4 main(vec2 fragCoord) {
			mainImage(out var fragColor, in fragCoord);
			return fragColor;
		}
	}
}
