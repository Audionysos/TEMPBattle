using Battle.binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace MStructTest {
	class Program {
		static void Main(string[] args) {
			var s = new Some();
			s.x = 10;
			s.y = 20;
			s.z = 1;
			s.c = (char)65;
			WriteLine(s.x + ":" + s.y + ":" + s.c);


			var o = new Other();
			o.a = 2;
			o.b = 4;

			s.other = o;

			WriteLine("s.other.b: " + s.other.b);
			s.other.b = 10;
			WriteLine("s.other.b: " + s.other.b);

			s.arr = new float[] { 1, 2, 3, 4, 5 };

			foreach (var n in s.arr) {
				WriteLine("n " + n);
			}

			WriteLine("c: " + s.c);
			WriteLine("z: " + s.z);


			/////////
			var b = new BS();
			var b2 = new BS2();
			b2.x = 5;
			b.s2 = b2;
			WriteLine("b.s2.x: " + b.s2.x);
			b.s2.x = 7;
			WriteLine("b.s2.x: " + b.s2.x);
		}
	}

	public class Some : MutableStruct {
		public double x {
			get => read<double>();
			set => write(value);
		}

		public int y {
			get => read<int>();
			set => write(value);
		}

		[ArrayInfo(3)]
		public float[] arr{
			get => readArray<float>();
			set => write(value);
		}

		public char c {
			get => read<char>();
			set => write(value);
		}

		[ArrayInfo(2)]
		public ArraySegment<int> arr2 {
			get => read<ArraySegment<int>>();
			set => write(value);
		}

		public Other other {
			get => readM<Other>();
			set => writeM(value);
		}

		public float z {
			get => read<float>();
			set => write(value);
		}

		//[ArrayInfo(3)]
		//public array<float> arr2 {
		//	get => read<array<float>>();
		//	set => write(value);
		//}

	}

	public class Other : MutableStruct {
		public int a {
			get => read<int>();
			set => write(value);
		}

		public double b {
			get => read<double>();
			set => write(value);
		}
	}

	public struct BS {
		public BS2 s2;
	}

	public struct BS2 {
		public int x;
	}

	
}
