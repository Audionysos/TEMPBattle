using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Battle {
	public class CallTest {

		private static CallContextControl control = new CallContextControl();

		public CallTest() {
			control.before = (c) => { Debug.WriteLine($"Calling the \"{c.method.Name}\" method."); };
			control.wrap = (c, a) => {
				try { a(); } catch (Exception e) {
					Debug.WriteLine($"Exception occured during performin the call to {c.method.Name}:" + e.ToString());
				}
			};
			control.after = (c) => { Debug.WriteLine($"Call to \"{c.method.Name}\" has ended."); };

			Debug.WriteLine(control.call(equals, "abc", "AbC"));
			control.call(greetings);
			control.call(error);
			control.call(yabada, 20d, 1);
			control.call(pa, 20d, new int[] { 1 });

			control.defer(equals, "abc", "Abc");
		}

		public bool equals(string s1, string s2) {
			return s1.ToUpper() == s2.ToUpper();
		}

		private void greetings() {
			Debug.WriteLine("Hello world");
		}

		public string yabada(double x, int y) {
			return (x * y).ToString();
		}

		public int pa(double x, params int[] par) => 0;

		public void error() => throw new Exception("Test exception");

	}

	public class CallContextControl {

		public Action<CallContextControl> before = c => { };
		public Action<CallContextControl, Action> wrap = (c, a) => a();
		public Action<CallContextControl> after = c => { };

		private void proceed(Action a) { before(this); wrap(this, a); after(this); }
		private R proceed<R>(MethodInfo m, Func<R> a) {
			method = m;
			before(this);
			R r = default; wrap(this, () => r = a());
			after(this);
			return r;
		}

		public MethodInfo method;
		public R call<P, P2, R>(Func<P, P2, R> func, P p, P2 p2)
			=>proceed(func.Method, () => func(p, p2));

		public void defer<P, P2, R>(Func<P, P2, R> func, P p, P2 p2) {
			var df = new DefferedCallResult<P, P2, R>(func, p, p2);
			df.invoke();
		}

		public void call(Action func) { method = func.Method; proceed(func); }

		#region Funcs
		public R call<R>(Func<R> func)
			=> proceed(func.Method, () => func());

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="P1"></typeparam>
		/// <typeparam name="R"></typeparam>
		/// <param name="func"></param>
		/// <param name="p1"></param>
		/// <returns></returns>
		public R call<P1, R>(Func<P1, R> func, P1 p1)
			=> proceed(func.Method, () => func(p1));

		//public R call<P1, P2, R>(Func<P1, P2, R> func, P1 p1, P2 p2)
		//	=> proceed(func.Method, () => func(p1, p2));

		public R call<P1, P2, P3, R>(Func<P1, P2, P3, R> func, P1 p1, P2 p2, P3 p3)
			=> proceed(func.Method, () => func(p1, p2, p3));

		public R call<P1, P2, P3, P4, R>(Func<P1, P2, P3, P4, R> func, P1 p1, P2 p2, P3 p3, P4 p4)
			=> proceed(func.Method, () => func(p1, p2, p3, p4));

		public R call<P1, P2, P3, P4, P5, R>(Func<P1, P2, P3, P4, P5, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5));

		public R call<P1, P2, P3, P4, P5, P6, R>(Func<P1, P2, P3, P4, P5, P6, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6));

		public R call<P1, P2, P3, P4, P5, P6, P7, R>(Func<P1, P2, P3, P4, P5, P6, P7, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9, P10 p10)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9, P10 p10, P11 p11)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9, P10 p10, P11 p11, P12 p12)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9, P10 p10, P11 p11, P12 p12, P13 p13)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9, P10 p10, P11 p11, P12 p12, P13 p13, P14 p14)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14));

		public R call<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, R>(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, R> func, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9, P10 p10, P11 p11, P12 p12, P13 p13, P14 p14, P15 p15)
			=> proceed(func.Method, () => func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15));

		#endregion

	}

	public class DefferedCallResult<P1, P2, R> {
		private Func<P1, P2, R> func;
		private P1 p1;
		private P2 p2;
		public DefferedCallResult(Func<P1, P2, R> func, P1 p1, P2 p2) {
			this.p1 = p1;
			this.p2 = p2;
			this.func = func;
		}
		public R invoke() => func(p1, p2);
	}

	//public class DefferedCallResult<R> {
	//	private Func<R> func;
	//	public DefferedCallResult(Func<R> func) {
	//		this.func = func;
	//	}
	//	public R invoke() => func();
	//}

	//public class DefferedCallResult<P1, R> {
	//	private Func<P1, R> func;
	//	private P1 p1;
	//	public DefferedCallResult(Func<P1, R> func, P1 p1) {
	//		this.p1 = p1; this.func = func;
	//	}
	//	public R invoke() => func(p1);
	//}

	////public class DefferedCallResult<P1, P2, R> {
	////	private Func<P1, P2, R> func;
	////	private P1 p1;
	////	private P2 p2;
	////	public DefferedCallResult(Func<P1, P2, R> func, P1 p1, P2 p2) {
	////		this.p1 = p1;
	////		this.p2 = p2; this.func = func;
	////	}
	////	public R invoke() => func(p1, p2);
	////}

	///// <summary>Stores given function with specific arguments to be invoked later.</summary>
	///// <typeparam name="P1">Type of the parameter of target function.</typeparam>
	///// <typeparam name="P2"></typeparam>
	///// <typeparam name="P3"></typeparam>
	///// <typeparam name="R">Return type of target function.</typeparam>
	//public class DefferedCallResult<P1, P2, P3, R> {
	//	/// <summary>Function to invoke later.</summary>
	//	private Func<P1, P2, P3, R> func;
	//	/// <summary>Object to be passed as function parameter.</summary>
	//	private P1 p1;
	//	private P2 p2;
	//	private P3 p3;
	//	/// <summary>Creates new instance of deffered function.</summary>
	//	/// <param name="func">Target function to be called later.</param>
	//	/// <param name="p1">Parameter that will be passed to target function when it will be invoked.</param>
	//	/// <param name="p2"></param>
	//	/// <param name="p3"></param>
	//	public DefferedCallResult(Func<P1, P2, P3, R> func, P1 p1, P2 p2, P3 p3) {
	//		this.p1 = p1;
	//		this.p2 = p2;
	//		this.p3 = p3; this.func = func;
	//	}
	//	/// <summary>Invokes method with arguments, specifed in constructor.</summary>
	//	/// <returns>Function result.</returns>
	//	public R invoke() => func(p1, p2, p3);
}




public class Generator {


		public Generator() {
			var fs = new StringBuilder("#region Funcs\n");
			var dcs = new StringBuilder("#region Defferred classes\n");
			var ac = 16;
			for (int cc = 0; cc <= ac; cc++) {
				var argTypes = "P{0}, ".x(cc, id);
				var f = $"public R call<{argTypes}R>(Func<{argTypes}R> func{", ".If("P{0} p{0}, ".x(cc, id).b(2))})\n" +
						$"	=> proceed(func.Method, () => func({"p{0}, ".x(cc, id).b(2)}));";
				fs.Append(f).Append("\n\n");

				var parDesc = $"	/// <summary>Object to be passed as function parameter.</summary>\n"; 

				var dc =	$"/// <summary>Stores given function with specific arguments to be invoked later.</summary>\n" +
							$"{"/// <typeparam name=\"P{0}\">Type of the parameter of target function.</typeparam>\n".x(cc, id)}" +
							$"/// <typeparam name=\"R\">Return type of target function.</typeparam>\n" +
							$"public class DefferedCallResult<{argTypes}R> {{\n" +
							$"	/// <summary>Function to invoke later.</summary>\n" +
							$"	private Func<{argTypes}R> func;\n" +
						$"{(parDesc+"	private P{0} p{0};\n").x(cc, id)}" +
							$"	/// <summary>Creates new instance of deffered function.</summary>\n" +
							$"	/// <param name=\"func\">Target function to be called later.</param>\n" +
						$"{"	/// <param name=\"p{0}\">Parameter that will be passed to target function when it will be invoked.</param>\n".x(cc, id)}" +
							$"	public DefferedCallResult(Func<{argTypes}R> func{", ".If("P{0} p{0}, ".x(cc, id).b(2))}) {{\n" +
						$"{"		this.p{0} = p{0};\n".x(cc, id)}" +
							$"		this.func = func;\n" +
							$"	}}\n" +
							$"	/// <summary>Invokes method with arguments, specifed in constructor.</summary>\n" +
							$"	/// <returns>Target function result.</returns>\n" +
							$"	public R invoke()=> func({"p{0}, ".x(cc, id).b(2)});\n" +
							$"}}";
				dcs.Append(dc).Append("\n\n");
			}
			fs.Append("#endregion");
			dcs.Append("#endregion");
			Debug.WriteLine(fs);
			Clipboard.SetText(dcs.ToString());
		}
		private object id(int c) => c + 1; 
}

	public static class StringExt{
		public static string x(this String s, int c, params Func<int, object>[] acts) {
			var r = "";
			var reps = findReplacements(s);
			if (reps.Length > 0 && acts == null || acts.Length == 0) throw new Exception("String specifies replacements but no functions were given for replacements.");
			for (int i = 0; i < c; i++) {
				var cs = s; var io = 0;
				foreach (var p in reps) {
					if(p.ri >= acts.Length) throw new Exception($"Replacment function for index {p.ri} was not specifed.");
					cs = cs.Substring(0, p.oi + io) + acts[p.ri](i) + cs.Substring(p.ci+1 + io);
					io = cs.Length - s.Length;
				}
				r += cs;
			}
			return r;
		}

		public static string b(this String s, int c)
			=> s.Length > c ? s.Substring(0, s.Length - c) : "";

		public static string If(this String s, String o)
			=> string.IsNullOrEmpty(o) ? "" : s + o;

		/// <summary></summary>
		/// <param name="s"></param>
		/// <returns></returns>
		private static StringRep[] findReplacements(string s) {
			var oi = -1; var ci = -1; var id = "";
			var reps = new List<StringRep>();
			for (int i = 0; i < s.Length; i++) {
				var c = s[i].ToString();
				if (c == "{") {
					if (oi >= 0) oi = -1;
					else oi = i;
				}else if(c == "}") {
					if(oi >= 0) {
						var p = int.TryParse(id, out int ri);
						if (!p) throw new Exception($"Could not parser \"{{{id}}}\" to int index.");
						var r = new StringRep() {
							oi = oi,
							ci = i,
							ri = ri
						};
						reps.Add(r);
						oi = ci = -1;
					}
					else if (ci >= 0) oi = -1;
					else ci = i;
				}else if(oi > 0) id += c;
			}
			return reps.ToArray();
		}
	}

	public class StringRep {
		/// <summary>Open index.</summary>
		public int oi;
		/// <summary>Close index.</summary>
		public int ci;
		/// <summary>Replacement index.</summary>
		public int ri;
	}

