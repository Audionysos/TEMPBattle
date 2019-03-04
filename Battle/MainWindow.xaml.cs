using Battle.binary;
using Battle.coord;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Battle {
	//public static class TabsExt {
	//	public static void process<T, A>(this T s) where T : MutableStruct, ITab<A> {

	//	}
	//}


	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		#region Structs template variables
		private Math math = new Math();
		private Alphabet alpha = new Alphabet();
		private Parameter param = new Parameter();
		private ParameterElement elem = new ParameterElement();
		#endregion

		private List<(int code, MutableStruct struc, Action action)> processors;
		/// <summary>Input stream.</summary>
		private Stream ins;
		/// <summary>Output stream.</summary>
		private Stream os;
		/// <summary>Current struct position in the stream.</summary>
		private int cp = 0;

		public MainWindow() {
			InitializeComponent();
			//var ct = new CallTest();
			//var g = new Generator();
			var rt = new SubrangeTest();

			return;


			processors = new List<(int code, MutableStruct struc, Action action)>() {
				(0, math, processMath),
				(1, alpha, processAlpabet),
				(2, param, processParameter)
			};
			defineHandlers();
			var x = math.reserved0;
			var px = param.reserved0;

			ins = File.Open("test", FileMode.OpenOrCreate);
			os = File.Open("test_wynik", FileMode.OpenOrCreate);

			writeTestData(4);

			os.SetLength(ins.Length);
			readStream();

			startTest();
			
			return;
			testData();

			ins.Close();
			os.Close();
		}

		private void startTest() {
			cp = 0;
			ins.Seek(cp, SeekOrigin.Begin);
			os.Seek(cp, SeekOrigin.Begin);
			comp.navi.next.Click += (s, e) => { testNextStuct(); };
			testNextStuct();
			//var vb = new Bytes(1000);
			//bvA.bytes = vb.read.fromStream(ins, 0).back;
		}

		private void defineHandlers() {
			var r = new Random();
			handlers = new List<StructHander>() {
				new StructHandler<Math, double>() {
					action = s => s.process((i, c) => i + c++),// processTabs<Math, double>(s, (i, c) => i + c++),
					check = (sA, sB) => sA.compareTabs(sB, (av, bv) => av + 0 == bv),
					generate = s => s.process( i => r.NextDouble() * 1000),
				},
				new StructHandler<Alphabet, char>() {
					action = s => processTabs<Alphabet, char>(s, c => ++c),
					check = (sA, sB) =>
						compareTabs<Alphabet, char>(sA, sB,
							(a, b) => b - a == 1),
					generate = s
						=> processTabs<Alphabet, char>(s, i => randomChar("a", "n"))
				},
				new StructHandler<Parameter, ParameterElement>() {
					action = s =>
						processTabs<Parameter, ParameterElement>(param,
							e => {
								e.bytes.at(0).modify<int>(i => i % 100);
								return e;
							}
						),
					check = (sA, sB) =>
						compareTabs<Parameter, ParameterElement>(sA, sB,
							(a, b) => compare(
								a.bytes.read.array<int>(),
								b.bytes.read.array<int>(),
								(ia, ib) => ia % 100 == ib)
							.Count == 0
						),
				},
			};
		}

		private Random r = new Random();
		private char randomChar(string sl, string el) {
			var sc = sl.ToCharArray()[0];
			var ec = el.ToCharArray()[0];
			return (char)r.Next(sc, ec);
		}

		private List<StructHander> handlers;

		#region Handler
		/// <summary>Provides methods for processing, checking and and producing new structs of single, specific type.</summary>
		public abstract class StructHander {
			/// <summary>Initial struct instance</summary>
			public abstract MutableStruct mutable { get; }
			public abstract void process();
			public abstract List<Exception> compare(MutableStruct a, MutableStruct b);
			/// <summary>Get new sturct o the same type as handler's base struct.</summary>
			/// <returns></returns>
			public abstract MutableStruct getNewStruct();
			/// <summary>Invokes method that should populate struct bytes.</summary>
			public abstract void populate();
			public abstract Type tabType { get; }
		}
		/// <summary>Specifies algoriths for handling the struct of given stuct type.</summary>
		/// <typeparam name="T">Taype of the struct</typeparam>
		/// <typeparam name="A">Type of <see cref="ITab{T}.tab"/> array elments the struct contain.</typeparam>
		public class StructHandler<T, A> : StructHander where T : MutableStruct, ITab<A> {
			private static Random r;
			override public MutableStruct mutable => struc;
			public T struc { get; private set; }
			/// <summary>Actions to be performen on each struct object encountered in the stream.</summary>
			public Action<T> action { get; set; }
			public Func<T,T, List<Exception>> check { get; set; }
			/// <summary>Generate data for the struct with this action.</summary>
			public Action<T> generate { get; set; }

			public override Type tabType => typeof(A);

			public StructHandler() => struc = Activator.CreateInstance<T>();

			public override void process() => action(struc);
			public override List<Exception> compare(MutableStruct a, MutableStruct b)
				=> check(a as T, b as T);
			public override MutableStruct getNewStruct()
				=> Activator.CreateInstance<T>();

			public override void populate() => generate?.Invoke(struc);
		}
		#endregion

		#region Testing
		private void testData() {
			cp = 0; ins.Seek(cp, SeekOrigin.Begin);
			MutableStruct i; MutableStruct o; //input and output structs to be compared.
			while (Bytes.Read(ins, out int c) > 0) {
				if (c > 2 || c < 0) break; //end of objects
				var h = handlers[c]; //current object
				i = h.getNewStruct().Load(ins, cp);
				o = h.getNewStruct().Load(os, cp);
				compareStructs(i, o, c);
				h.compare(i, o);
				cp += i.size;
			}
		}

		public bool testNextStuct() {
			MutableStruct i; MutableStruct o;
			ins.Seek(cp, SeekOrigin.Begin);
			Bytes.Read(ins, out int c);
			if (c > 2 || c < 0) return false; //end of objects
			var h = handlers[c]; //current object
			i = h.getNewStruct().Load(ins, cp);
			o = h.getNewStruct().Load(os, cp);
			bvA.bytes = i.bytes;
			bvB.bytes = i.bytes;

			comp.structInfo = i.definition;
			comp.sturctA = i;
			comp.sturctB = o;
			//compareStructs(i, o, c);
			//h.compare(i, o);
			cp += i.size;
			return true;
		}

		/// <summary>Base comparition of <see cref="ITab{T}"/> structs.</summary>
		/// <param name="i"></param>
		/// <param name="o"></param>
		private void compareStructs(MutableStruct i, MutableStruct o, int code) {
			if ((i as ITab).code != code) throw new Exception("Input struct don't contain valid code.");
			if ((o as ITab).code != code) throw new Exception("Output struct don't contain valid code.");
			if (i.address != o.address) throw new Exception("Structs addresses are different.");
		}

		#region Generic Comparision
		public List<Exception> compareTabs<T, AT>(T a, T b, Func<AT, AT, bool> comp)
			where T : MutableStruct, ITab<AT>
		{
			var exs =compare((a as T).tab, (b as T).tab, comp);
			exs.AddRange(compare(otherTabs<T,AT>(a), otherTabs<T,AT>(b), comp));
			return exs;
		}
		public List<Exception> compareTabs<T, AT>(T a, T b, Func<AT, AT, int, bool> comp)
			where T : MutableStruct, ITab<AT> {
			var c = 0;
			return compareTabs<T, AT>(a, b, (aa, bb) => comp(aa, bb, c++));
		} 

		public static AT[] otherTabs<T, AT>(T s) where T : MutableStruct, ITab<AT>
		=> new Bytes(s.size_additional_tab)
			.read.fromStream(s.stream, s.offset_additional_tab)
			.at(0).array<AT>();

		/// <summary>Compare two arrays with specified formula.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static List<Exception> compare<T>(T[] a, T[] b, Func<T,T, bool> comp) {
			var exs = new List<Exception>();
			if (a.Length != b.Length) { exs.Add(new Exception("Arrays sizes are differen"));
				return exs;}
			for (int i = 0; i < a.Length; i++)
				if(!comp(a[i], b[i])) exs.Add(new Exception("Incorrect value"));
			return exs;
		}
		private List<Exception> compare<T>(Bytes a, Bytes b, Func<T, T, bool> comp)
			=> compare(a.at(0).read.array<T>(), b.at(0).read.array<T>(), comp);
		#endregion

		//private TabStruct readTabStruct(Type t, Stream str, long at) {
		//	var s = Activator.CreateInstance(t) as MutableStruct;
		//	s.Load(str, at);
		//	var tbs = s as ITab;
		//	var add = new Bytes(tbs.size_additional_tab);
		//	add.read.fromStream(in, tbs.offset_additional_tab);
		//	return new TabStruct(s, add);
		//}

		//public class TabStruct {
		//	public MutableStruct struc { get; private set; }
		//	public ITab itab { get => struc as ITab; }
		//	public Bytes adds { get; private set; }
		//	public TabStruct(MutableStruct s, Bytes additonalBytes) {
		//		this.struc = s;
		//		this.adds = additonalBytes;
		//	}
		//}

		private void writeTestData(int count) {
			var r = new Random(); var cp = 0;
			var mss = 0;//max struct size
			handlers.ForEach(h => {
				h.mutable.stream = ins;
				(h.mutable as ITab).outStream = ins;
				mss = h.mutable.size > mss ? h.mutable.size : mss;
			});
			Bytes.Write(ins, int.MaxValue, mss * count); //start additional data with unrecognized struct code.
			var ado = mss * count + sizeof(int);//additonal data offset. Starts safe offset.
			ins.SetLength(ado);
			for (int i = 0; i < count; i++) {
				var c = r.Next(0, 1);
				var h = handlers[c];
				//h.mutable.Load(ins, cp);
				h.mutable.Save(cp);
				var it = h.mutable as ITab;
				it.size_additional_tab = r.Next(0, 100);
				it.offset_additional_tab = ado;
				h.populate();
				h.mutable.Save(cp);
				cp += h.mutable.size;
				ado += it.size_additional_tab * binary.Size.Of(h.tabType);
			}
		}
		#endregion

		#region processors
		private void processMath() => processTabs<Math, double>(math, (i, c) => i + c++);

		private void processAlpabet()
			=> processTabs<Alphabet, char>(alpha, i => i += (char)1);

		private void processParameter()
			=> processTabs<Parameter, ParameterElement>(param,
				e => {
					e.bytes.at(0).modify<int>(i => i % 100);
					return e;
				}
			);
		#endregion


		#region Processing
		private void readStream() {
			handlers.ForEach(h => (h.mutable as ITab).outStream = os);
			ins.Seek(cp, SeekOrigin.Begin);
			while (Bytes.Read(ins, out int c) > 0) {
				if (c > 2 || c < 0) break; //end of objects
				var h = handlers[c]; //current object
				h.mutable.Load(ins, cp);
				h.process();
				cp += h.mutable.size;
				ins.Seek(cp, SeekOrigin.Begin);
			}
		}


		/// <summary>Use given function to process each <see cref="ITab{T}.tab"/> item and all additional items at <see cref="ITab{T}.offset_additional_tab"/>.</summary>
		/// <typeparam name="T">Struct containing tab.</typeparam>
		/// <typeparam name="A">Type of tab array.</typeparam>
		/// <param name="s">struct instance</param>
		/// <param name="p">proccesing formula</param>
		private void processTabs<T, A>(T s, Func<A,A> p)
			where T : MutableStruct, ITab<A>
		{
			var v = s.definition[nameof(ITab<A>.tab)]; //modify tab array
			s.bytes.at(v.offset).modify(p, v.size);
			s.bytes.at(0).write.ToStream(os, cp);

			var ob = new Bytes(s.size_additional_tab); //other bytes
			ob.read.fromStream(ins, s.offset_additional_tab);
			ob.at(0).modify(p);
			ob.at(0).write.ToStream(os, s.offset_additional_tab);
		}

		/// <summary>Use given function to process each <see cref="ITab{T}.tab"/> item.
		/// Second argument is the tab counter.</summary>
		/// <typeparam name="T">Struct containing tab.</typeparam>
		/// <typeparam name="A">Type of tab array.</typeparam>
		/// <param name="s">Struct instance</param>
		/// <param name="p">Proccesing formula. Second argument is item counter.</param>
		private void processTabs<T, A>(T s, CountedTabProcessor<A> p)
			where T : MutableStruct, ITab<A>
		{
			var c = 0;
			processTabs<T,A>(s, v => p(v, c++));
		}
		public delegate T CountedTabProcessor<T>(T inputElement, int counter);

		#endregion
	}

	#region Structs

	#region Base interface 
	public interface ITab {
		int code { get; }
		int size_additional_tab { get; set; }
		int offset_additional_tab { get; set; }
		Stream outStream { get; set; }
	}
	public interface ITab<T> : ITab{
		T[] tab { get; set; }
		void process(Func<T, T> t);
	}
	public abstract class BaseTab<T> : MutableStruct, ITab<T> {
		public abstract T[] tab { get; set; }
		public Stream outStream { get; set; } 

		public abstract int code { get; protected set; }
		public abstract int size_additional_tab { get; set; }
		public abstract int offset_additional_tab { get; set; }

		public void process(Func<T, T> t) {
			var v = definition[nameof(tab)];
			bytes.at(v.offset).modify(t, v.dimesnions[0]);
			bytes.at(0).write.ToStream(outStream, address);

			var ob = new Bytes(size_additional_tab); //other bytes
			ob.read.fromStream(stream, offset_additional_tab);
			ob.at(0).modify(t);
			ob.at(0).write.ToStream(outStream, offset_additional_tab);
		}

		public void process(Func<T, int, T> t) {
			var c = 0; process(v => t(v, c++));
		}

		public List<Exception> compareTabs(BaseTab<T> o, Func<T, T, bool> c) {
			var exs = MainWindow.compare(this.tab, o.tab, c);
			exs.AddRange(MainWindow.compare(this.otherTabs(), o.otherTabs(), c));
			return exs;
		}

		public T[] otherTabs()
			=> new Bytes(size_additional_tab)
				.read.fromStream(stream, offset_additional_tab)
				.at(0).array<T>();
	}
	#endregion

	public class ParameterElement : MutableStruct {
		public int a {
			get => read<int>(nameof(a));
			set => write(value, nameof(a));
		}
		public int b {
			get => read<int>(nameof(b));
			set => write(value, nameof(b));
		}
		public int c {
			get => read<int>(nameof(c));
			set => write(value, nameof(c));
		}
		public int d {
			get => read<int>(nameof(d));
			set => write(value, nameof(d));
		}
		public int e {
			get => read<int>(nameof(e));
			set => write(value, nameof(e));
		}
		public int f {
			get => read<int>(nameof(f));
			set => write(value, nameof(f));
		}

	}

	public class Parameter : MutableStruct, ITab<ParameterElement> {
		public Parameter() => code = 2;

		public int code {
			get => read<int>(nameof(code));
			private set => write(value, nameof(code));
		}

		#region reserved
		[ArrayInfo(7)]
		public char[] reserved0 {
			get => readArray<char>(nameof(reserved0));
			set => write(value, nameof(reserved0));
		}

		public int reserved1 {
			get => read<int>(nameof(reserved1));
			set => write(value, nameof(reserved1));
		}

		public char reserved2 {
			get => read<char>(nameof(reserved2));
			set => write(value, nameof(reserved2));
		}

		public double reserved3 {
			get => read<double>(nameof(reserved3));
			set => write(value, nameof(reserved3));
		}

		public float reserved4 {
			get => read<float>(nameof(reserved4));
			set => write(value, nameof(reserved4));
		}

		public int reserved5 {
			get => read<int>(nameof(reserved5));
			set => write(value, nameof(reserved5));
		}

		public char reserved6 {
			get => read<char>(nameof(reserved6));
			set => write(value, nameof(reserved6));
		}
		#endregion

		[ArrayInfo(10)]
		public ParameterElement[] tab {
			get => readArray<ParameterElement>(nameof(tab));
			set => write(value, nameof(tab));
		}

		#region tabInfo
		public int size_additional_tab {
			get => read<int>(nameof(size_additional_tab));
			set => write(value, nameof(size_additional_tab));
		}

		public int offset_additional_tab {
			get => read<int>(nameof(offset_additional_tab));
			set => write(value, nameof(offset_additional_tab));
		}
		public Stream outStream { get; set; }

		public void process(Func<ParameterElement, ParameterElement> t) {
			throw new NotImplementedException();
		}
		#endregion
	}

	public class Alphabet : MutableStruct, ITab<char> {
		public Alphabet() => code = 1;

		public int code {
			get => read<int>(nameof(code));
			private set => write(value, nameof(code));
		}

		#region reserved
		public int reserved0 {
			get => read<int>(nameof(reserved0));
			set => write(value, nameof(reserved0));
		}

		public char reserved1 {
			get => read<char>(nameof(reserved1));
			set => write(value, nameof(reserved1));
		}

		[ArrayInfo(11)]
		public float[] reserved2 {
			get => readArray<float>(nameof(reserved2));
			set => write<float>(value, nameof(reserved2));
		}

		public double reserved3 {
			get => read<double>(nameof(reserved3));
			set => write(value, nameof(reserved3));
		}

		public double reserved4 {
			get => read<double>(nameof(reserved4));
			set => write(value, nameof(reserved4));
		}

		public char reserved5 {
			get => read<char>(nameof(reserved5));
			set => write(value, nameof(reserved5));
		}
		#endregion

		[ArrayInfo(10)]
		public char[] tab {
			get => readArray<char>(nameof(tab));
			set => write(value, nameof(tab));
		}

		#region tabInfo
		public int size_additional_tab {
			get => read<int>(nameof(size_additional_tab));
			set => write(value, nameof(size_additional_tab));
		}

		public int offset_additional_tab {
			get => read<int>(nameof(offset_additional_tab));
			set => write(value, nameof(offset_additional_tab));
		}
		public Stream outStream { get; set; }

		public void process(Func<char, char> t) {
			throw new NotImplementedException();
		}
		#endregion
	}

	public class Math : BaseTab<double> {
		public Math() => code = 0;

		public override int code {
			get => read<int>(nameof(code));
			protected set => write(value, nameof(code));
		}

		#region reserved
		[ArrayInfo(7)]
		public int[] reserved0 {
			get => readArray<int>(nameof(reserved0));
			set => write(value, nameof(reserved0));
		}

		public char reserved1 {
			get => read<char>(nameof(reserved1));
			set => write(value, nameof(reserved1));
		}

		public char reserved2 {
			get => read<char>(nameof(reserved2));
			set => write(value, nameof(reserved2));
		}

		public char reserved3 {
			get => read<char>(nameof(reserved3));
			set => write(value, nameof(reserved3));
		}

		public float reserved4 {
			get => read<float>(nameof(reserved4));
			set => write(value, nameof(reserved4));
		}

		public int reserved5 {
			get => read<int>(nameof(reserved5));
			set => write(value, nameof(reserved5));
		}

		public char reserved6 {
			get => read<char>(nameof(reserved6));
			set => write(value, nameof(reserved6));
		}
		#endregion

		[ArrayInfo(10)]
		public override double[] tab {
			get => readArray<double>(nameof(tab));
			set => write(value, nameof(tab));
		}

		#region tabInfo
		public override int size_additional_tab {
			get => read<int>(nameof(size_additional_tab));
			set => write(value, nameof(size_additional_tab));
		}

		public override int offset_additional_tab {
			get => read<int>(nameof(offset_additional_tab));
			set => write(value, nameof(offset_additional_tab));
		}
		#endregion
	}
	#endregion


}
