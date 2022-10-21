using static Battle.reflection.M;

namespace Battle.reflection.shaders {
	//https://www.shadertoy.com/view/4d2XWV
	public class Sphere : Shader {

		float sphIntersect(in vec3 ro, in vec3 rd, in vec4 sph) {
			vec3 oc = ro - sph.xyz;
			float b = dot(oc, rd);
			float c = dot(oc, oc) - sph.w * sph.w;
			float h = b * b - c;
			if (h < 0.0) return -1.0f;
			return -b - sqrt(h);
		}

		//https://www.youtube.com/watch?v=HFPlKQGChpE
		float sphIntersect2(in vec3 ro, in vec3 rd, in vec4 sph) {
			var s = sph.xyz; var r = sph.w;
			var t = dot(s-ro, rd);
			var p = ro + rd * t;
			var y = length(s - p);
			if(y < r) {
				var x = sqrt(r * r - y * y); 
				var t1 = t - x;
				var t2 = t + x;
				var p1 = length(rd * t1);
				var p2 = length(rd * t2);
				return min(p1, p2);
			}
			return -1;
		}

		vec3 sphNormal(in vec3 pos, in vec4 sph) {
			return normalize(pos - sph.xyz);
		}

		float sphOcclusion(in vec3 pos, in vec3 nor, in vec4 sph) {
			vec3 r = sph.xyz - pos;
			float l = length(r);
			return dot(nor, r) * (sph.w * sph.w) / (l * l * l);
		}

		float sphSoftShadow(in vec3 ro, in vec3 rd, in vec4 sph, in float k) {
			vec3 oc = ro - sph.xyz;
			float b = dot(oc, rd);
			float c = dot(oc, oc) - sph.w * sph.w;
			float h = b * b - c;
			return (b > 0.0f) ? (float)step(-0.0001, c) : (float)smoothstep(0.0, 1.0, h * k / b);
		}

		float iPlane(in vec3 ro, in vec3 rd) {
			return (-1.0f - ro.y) / rd.y;
		}

		override protected void mainImage(out vec4 fragColor, in vec2 fragCoord) {
			vec2 p = (2.0 * fragCoord.xy - iResolution.xy) / iResolution.y;
			vec3 ro = vec3(0.0, 0.0, 4.0);
			vec3 rd = normalize(vec3(p, -2.0));
			// sphere animation
			vec4 sph = vec4(vec3(0, 0.0, 0.0), 1.0);
			vec3 lig = normalize(vec3(0.6, 0.3, 0.4));
			vec3 col = vec3(0.0);

			float tmin = 1e10f;
			vec3 nor = new vec3();
			float occ = 1.0f;

			float t1 = iPlane(ro, rd);
			if (t1 > 0.0) {
				tmin = t1;
				vec3 pos = ro + t1 * rd;
				nor = vec3(0.0, 1.0, 0.0);
				occ = 1.0f - sphOcclusion(pos, nor, sph);
			}
			float t2 = sphIntersect2(ro, rd, sph);
			if (t2 > 0.0 && t2 < tmin) {
				tmin = t2;
				vec3 pos = ro + t2 * rd;
				nor = sphNormal(pos, sph);
				occ = 0.5f + 0.5f * nor.y;
			}
			if (tmin < 1000.0) {
				vec3 pos = ro + tmin * rd;

				col = vec3(1.0);
				col *= clamp(dot(nor, lig), 0.0, 1.0);
				col *= sphSoftShadow(pos, lig, sph, 2.0f);
				col += 0.05 * occ;
				col *= exp(-0.05 * tmin);
			}

			col = sqrt(col);
			fragColor = vec4(col, 1.0);
		}
	}

	#region other
	//maze
	//buffer.perPixel(fragCoord => {
	//	//var uv = fragCoord / buffer.size;
	//	var uv = (fragCoord - 0.5 * iResolution.xy) / iResolution.y;
	//	//var uv = (fragCoord-.5*iResolution.xy) / iResolution.y;
	//	var col = new float3();
	//	//uv -= .5;
	//	uv *= 30;

	//	//if (uv.x > 1.4 && uv.y < 1.45) Debugger.Break();
	//	var gv = uv.fract() - .5;
	//	var id = uv.floor();
	//	var n = hash21(id);

	//	var w = 0.08;
	//	if (n < .5) gv.x *= -1;
	//	var mask = smoothstep(.01, -.01, Abs(gv.x + gv.y) - w);
	//	//var mask = Abs(gv.x + gv.y);
	//	col += mask;

	//	//if (gv.x > .48 || gv.y > .48) col = (1, 0, 0);
	//	return (col, 1);// (uv.x, uv.y, 0, 1);
	//});

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
	#endregion
}
