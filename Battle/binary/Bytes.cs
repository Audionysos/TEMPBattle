using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Battle.binary {
	/// <summary>Simplifies manipulations on byte array.
	/// TODO: Allow specifing in constructor subrange of source array on which to oparate.</summary>
	public class Bytes {
		/// <summary>Source array.</summary>
		private byte[] s;
		/// <summary>Source array. The instance can change across lifespan of <see cref="Bytes"/> instance.</summary>
		public byte[] source => s;
		/// <summary>Current read/write position.</summary>
		private int p;
		/// <summary>Current read/write position.</summary>
		public int position {
			get => p;
			set => p = value;
		}
		/// <summary>Number of bytes available to read. Trying to read or wirte value which size exceeds this number will cause an error.</summary>
		public int available => s.Length - position;
		/// <summary>Cont of all bytes in <see cref="source"/> array, regardles of current position.</summary>
		public int Count => s.Length;
		/// <summary>Reader object used to read values from form source Bytes array.
		/// Current position is automatically increased each time a value is aquired.</summary>
		public BytesReader read { get; private set; }
		/// <summary>Writer object used to write values into soruce Bytes array.
		/// Current position is automatically increased each time a value is writed.</summary>
		public BytesWriter write { get; private set; }

		#region statics
		private static Bytes sBytes = new Bytes(64);
		private static BytesReader wr = new BytesReader(sBytes);
		public static T Read<T>(Stream s) {
			var z = Size.Of<T>();
			sBytes = sBytes.Count >= z ? sBytes : new Bytes(z);
			s.Read(sBytes.source, 0, z);
			return sBytes.read.data<T>();
		}

		/// <summary>Reads given variable form the string and returns number or bytes readed by the stream.</summary>
		/// <typeparam name="T">Type of wariable to read.</typeparam>
		/// <param name="s">Stream to read wariable value from.</param>
		/// <param name="o">Output variable to be set.</param>
		/// <returns>Number of byter readed from the stream.</returns>
		public static int Read<T>(Stream s, out T o) {
			var z = Size.Of<T>();
			sBytes = sBytes.Count >= z ? sBytes : new Bytes(z);
			sBytes.position = 0;
			var rc = s.Read(sBytes.source, 0, z);
			o = sBytes.read.data<T>();
			return rc;
		}

		public static void Write<T>(Stream s, T v, int at) {
			var z = Size.Of<T>();
			sBytes = sBytes.Count >= z ? sBytes : new Bytes(z);
			sBytes.write.at(0).data(v);
			s.Write(sBytes.source, 0, z);
		}
		#endregion

		/// <summary>Crate new bytes instance that warps and controls given array.</summary>
		/// <param name="array"></param>
		public Bytes(byte[] array) { s = array; init(); }
		/// <summary>Crate new instance of bytes creating new array of specified size.</summary>
		/// <param name="size"></param>
		public Bytes(int size) { s = new byte[size]; init(); }

		private void init() {
			read = new BytesReader(this);
			write = new BytesWriter(this);
		}

		#region Source changes
		/// <summary>Prepends specified array to this bytes array.
		/// New array is created with size of this <see cref="Count"/> + given header <see cref="T:byte[]"/> length and set as a cource.
		/// Current <see cref="position"/> is increased by <paramref name="header"/> Length. Data is copied to new array.</summary>
		/// <param name="header">Array to be preceede current source array. null is allowed as an argument.</param>
		/// <returns>This bytes</returns>
		public Bytes prepend(byte[] header, bool movePositon = true) {
			if (header == null) return this;
			var ns = new byte[s.Length + header.Length];
			Buffer.BlockCopy(header, 0, ns, 0, header.Length);
			Buffer.BlockCopy(s, 0, ns, header.Length, s.Length);
			s = ns;
			if (movePositon) p += header.Length;
			return this;
		}

		/// <summary>Appends specified array to this bytes array.
		/// New array is created with size of this <see cref="Count"/> + given footer <see cref="T:byte[]"/> length and set as a source.</summary>
		/// <param name="header">Array to instert after current source array. null is allowed as an argument.</param>
		/// <returns>This bytes</returns>
		public Bytes append(byte[] footer) {
			if (footer == null) return this;
			var ns = new byte[s.Length + footer.Length];
			Buffer.BlockCopy(s, 0, ns, 0, s.Length);
			Buffer.BlockCopy(footer, 0, ns, s.Length, footer.Length);
			s = ns;
			return this;
		}

		/// <summary>Creates larger <see cref="source"/> bytes array.
		/// Contents of current <see cref="source"/> array are copied to the beginning of a new one.</summary>
		/// <param name="additionalBytes">Number of new bytes to add. If given value is smaller than 1, new array will be twice the size of original one.</param>
		/// <returns>This bytes.</returns>
		public Bytes grow(int additionalBytes = 0) {
			var ns = new byte[s.Length + (additionalBytes > 0 ? additionalBytes : s.Length)];
			Buffer.BlockCopy(s, 0, ns, 0, s.Length);
			s = ns;
			return this;
		}
		#endregion

		/// <summary>Perform given operation in the array on each subsequent element of specific type.</summary>
		/// <typeparam name="T">Type to operate on.</typeparam>
		/// <param name="operation">Operation to be performed.</param>
		/// <param name="count">Number of elements to modified. If this argument is less than or equall 0, operation will be performed to the end of the bytes.</param>
		public void modify<T>(Func<T,T> operation, int count = 0) {
			var s = Size.Of<T>(); var c = 0;
			if (count == 0) count = int.MaxValue;
			while (available >= s && c < count) { c++;
				var ip = p; //read opertation shifts postion
				var m = operation(read.data<T>());
				write.at(ip).data(m);
			}
		}

		/// <summary>Sets position at which read/write will be performed.</summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Bytes at(int position = -1) { p = position; return this; }

		public static implicit operator byte[] (Bytes bs) => bs.s;
		public static implicit operator Bytes(byte[] ba) => new Bytes(ba);

		public override string ToString() {
			return $"[{available}/{Count}](p:{position})";
		}

		/// <summary>Provides methods and properties to read from parent <see cref="Bytes"/> array and automatically updates <see cref="Bytes.position"/>.</summary>
		public class BytesReader {
			/// <summary>Parent Bytes array.</summary>
			private Bytes b;
			internal BytesReader(Bytes b) { this.b = b; pp = b.position; }
			/// <summary>Previous postion.</summary>
			private int pp;

			/// <summary>Sets position at which read will be performed.</summary>
			/// <param name="position"></param>
			/// <returns></returns>
			public BytesReader at(int position) { b.p = position; return this; }

			public Int16 Short() { pp = b.p; b.p += sizeof(Int16); return BitConverter.ToInt16(b.s, pp); }
			public int Int() { pp = b.p; b.p += sizeof(int); return BitConverter.ToInt32(b.s, pp); }
			public uint uInt() { pp = b.p; b.p += sizeof(uint); return BitConverter.ToUInt32(b.s, pp); }
			public long Long() { pp = b.p; b.p += sizeof(long); return BitConverter.ToInt64(b.s, pp); }
			public ulong uLong() { pp = b.p; b.p += sizeof(ulong); return BitConverter.ToUInt64(b.s, pp); }
			public double Double() { pp = b.p; b.p += sizeof(double); return BitConverter.ToDouble(b.s, pp); }
			public char Char() { pp = b.p; b.p += sizeof(char); return BitConverter.ToChar(b.s, pp); }
			public float Float() { pp = b.p; b.p += sizeof(float); return BitConverter.ToSingle(b.s, pp); }
			public byte Byte() { pp = b.p; b.p += sizeof(byte); return b.s[pp]; }

			public T data<T>() {
				var t = typeof(T);
				if (t == typeof(int)) return (T)(object)Int();
				if (t == typeof(uint)) return (T)(object)uInt();
				if (t == typeof(long)) return (T)(object)Long();
				if (t == typeof(ulong)) return (T)(object)uLong();
				if (t == typeof(double)) return (T)(object)Double();
				if (t == typeof(char)) return (T)(object)Char();
				if (t == typeof(float)) return (T)(object)Float();
				if (t == typeof(byte)) return (T)(object)Byte();
				throw new Exception(t.Name + " read is not supported");
			}

			public object data(Type t) {
				if (t == typeof(int)) return Int();
				if (t == typeof(uint)) return uInt();
				if (t == typeof(long)) return Long();
				if (t == typeof(ulong)) return uLong();
				if (t == typeof(double)) return Double();
				if (t == typeof(char)) return Char();
				if (t == typeof(float)) return Float();
				if (t == typeof(byte)) return Byte();
				throw new Exception(t.Name + " read is not supported");
			}

			/// <summary>Read array of specified type starting from current <see cref="Bytes.position"/>.</summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="count"></param>
			/// <returns></returns>
			public T[] array<T>(int count) {
				var a = new T[count];
				for (int i = 0; i < count; i++)
					a[i] = data<T>();
				return a;
			}

			/// <summary>Read all <see cref="available"/> bytes at current position.</summary>
			/// <typeparam name="T"></typeparam>
			/// <returns></returns>
			public T[] array<T>()
				=> array<T>(b.available / Size.Of<T>());

			/// <summary>Loads bytes from given stream into parent <see cref="Bytes"/> object, starting at specified stream postion.</summary>
			/// <param name="s">Stream to load bytes from.</param>
			/// <param name="sp">Position in stream from wherer to strat read.</param>
			/// <param name="count">Number of bytes to read. This number cannot be greater than <see cref="Bytes.available"/>.</param>
			/// <returns>This reader object.</returns>
			public BytesReader fromStream(Stream s, long sp, int count = 0) {
				if (count == 0) count = b.available;
				if (count > b.available) throw new Exception("Specified count is greated than available Bytes size.");
				s.Seek(sp, SeekOrigin.Begin);
				s.Read(b.s, b.p, count);
				b.p += count;
				return this;
			}

			/// <summary>Copies part of bytes array to new array of spefiecifed count starting from current <see cref="Bytes.position"/>.</summary>
			/// <param name="count">Number of bytes to read.</param>
			public byte[] bytes(int count) {
				var bs = new byte[count];
				Buffer.BlockCopy(b.s, b.p, bs, 0, count);
				b.p += count;
				return bs;
			}

			/// <summary>Copies all available bytes form current <see cref="Bytes.position"/> to the end of the bytes array.</summary>
			/// <returns>Rest avlailable bytes or null if no more bytes are available.</returns>
			public byte[] bytes() {
				if (b.available < 1) return null;
				var bs = new byte[b.available];
				Buffer.BlockCopy(b.s, b.p, bs, 0, b.available);
				b.p += b.available;
				return bs;
			}

			/// <summary>Returns parent bytes.</summary>
			public Bytes back => b;
		}

		public class BytesWriter {
			private Bytes b;
			internal BytesWriter(Bytes b) => this.b = b;

			/// <summary>Sets position at which write will be performed.</summary>
			/// <param name="position"></param>
			/// <returns></returns>
			public BytesWriter at(int position) { b.p = position; return this; }

			/// <summary>Write specified number of bytes from other <see cref="Bytes.source"/> starting at current positon.
			/// <see cref="position"/> of both <see cref="Bytes"/> instance are actulaized.</summary>
			/// <param name="src"></param>
			public void bytes(Bytes src, int count) {
				Buffer.BlockCopy(src, src.p, b, b.p, count);
				b.p += count;
				src.p += count;
			}

			public void bytes(Bytes src) {
				var count = src.Count;
				Buffer.BlockCopy(src, src.p, b, b.p, count);
				b.p += count;
				src.p += count;
			}

			public void Short(Int16 v) { Buffer.BlockCopy(BitConverter.GetBytes(v),0, b.s, b.p, sizeof(Int16)); b.p += sizeof(Int16); }
			public void Int(int v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(int)); b.p += sizeof(int); }
			public void uInt(uint v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(uint)); b.p += sizeof(uint); }
			public void Long(long v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(long)); b.p += sizeof(long); }
			public void uLong(ulong v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(ulong)); b.p += sizeof(ulong); }
			public void Double(double v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(double)); b.p += sizeof(double); }
			public void Char(char v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(char)); b.p += sizeof(char); }
			public void Float(float v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b.s, b.p, sizeof(float)); b.p += sizeof(float); }

			/// <summary>Write contents of parent <see cref="Bytes"/> object at specifed postion in given stream.</summary>
			/// <param name="s">Stream to write into.</param>
			/// <param name="sp">Stream positon from where to strat writing.</param>
			/// <param name="count">Number of bytes to write.</param>
			public void ToStream(Stream s, long sp, int count = 0) {
				if (count == 0) count = b.available;
				if (count > b.available) throw new Exception("Specified count is greated than available Bytes size.");
				s.Seek(sp, SeekOrigin.Begin);
				s.Write(b.s, b.p, count);
			}

			public void data<T>(T o) {
				var t = typeof(T);
				if (t == typeof(int)) Int(Convert.ToInt32(o));
				else if (t == typeof(uint)) uInt(Convert.ToUInt32(o));
				else if (t == typeof(long)) Long(Convert.ToInt64(o));
				else if (t == typeof(ulong)) uLong(Convert.ToUInt64(o));
				else if (t == typeof(double)) Double(Convert.ToDouble(o));
				else if (t == typeof(char)) Char(Convert.ToChar(o));
				else if (t == typeof(float)) Float(Convert.ToSingle(o));
				else throw new Exception($@"""{t}"" type read is not supported");
			}
		}
	}

	public static class Size {
		public static int Of(object x) => Of(x.GetType());

		[ThreadStatic]
		private static List<Type> checks = new List<Type>();

		public static int Of(Type t) {
			if (t == typeof(byte)) return sizeof(byte);
			if (t == typeof(int)) return sizeof(int);
			if (t == typeof(uint)) return sizeof(uint);
			if (t == typeof(long)) return sizeof(long);
			if (t == typeof(ulong)) return sizeof(ulong);
			if (t == typeof(double)) return sizeof(double);
			if (t == typeof(char)) return sizeof(char);
			if (t == typeof(float)) return sizeof(float);
			if (typeof(MutableStruct).IsAssignableFrom(t)) {
				//TODO: detect cross type reference
				var eti = checks.IndexOf(t);
				if (eti >= 0) {
					throw new CrossRefferenceException($"{checks[eti]} is cross refferenced with {checks.Last()}. Cross struct refferences are only possible with adress pointers.");
				}checks.Add(t);
				var to = Activator.CreateInstance(t) as MutableStruct;
				checks.RemoveAt(checks.Count - 1);
				return to.size;
			}
			throw new Exception(t.Name + " read is not supported");
		}

		public static int Of<T>() => Of(typeof(T));

		public static int Of(float x) => sizeof(float);
		public static int Of(double x) => sizeof(double);
	}

	public class CrossRefferenceException : Exception {
		public CrossRefferenceException(string message) :
			base (message) {

		}
	}
}
