using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Battle.binary {
	/// <summary>
	/// Extneding class cannot not use "new" operator to hide base type properties. 
	/// </summary>
	public abstract class MutableStruct {
		#region Properties
		private static Dictionary<Type, StructInfo> structs = new Dictionary<Type, StructInfo>();
		/// <summary>Assigns given struct variable. Returs ture if the info was already defined.</summary>
		public static bool getDefinition(Type t, out StructInfo info) {
			if (structs.ContainsKey(t)) { info = structs[t]; return true; }
			info = new StructInfo();
			structs.Add(t, info);
			return false;
		}
		/// <summary>Provide information about struct variables.</summary>
		public StructInfo definition { get; private set; }
		/// <summary>Address in parent stream from where struct was loaded.</summary>
		public long _ad = -1;
		/// <summary>Address in parent stream from where struct was loaded.
		/// Changing this values will cause reload of whole struct. 
		/// If stuct's stream is not set or if it's not opened for read, an Error will be thrown.</summary>
		public long address {
			get => _ad;
			set {
				if (_pS == null) throw new Exception("The struc has no parent stream.");
				if (!_pS.CanRead) throw new Exception("Parent stream is not opened for read operation.");
				Load(_pS, value);
			}
		}
		/// <summary>Provides access to raw bytes of the struct.</summary>
		public Bytes bytes { get; private set; }
		/// <summary>Size of the struct in bytes.</summary>
		public int size => definition.size;
		/// <summary>Parent stream.</summary>
		private Stream _pS;
		/// <summary>Parent stream from where the struct was/will be loaded.
		/// If sturuct was loaded already, it will be realoaded at the same position in new stream.</summary>
		public Stream stream {
			get => _pS;
			set {
				_pS = value;
				if (_pS == null) { _ad = -1; return; }
				if (_pS.CanRead && _ad > 0) Load(_pS, _ad);
			}
		}
		#endregion

		protected MutableStruct() {
			initInfo();
			bytes = new Bytes(definition.size);
		}

		private void initInfo() {
			var t = this.GetType();
			if (getDefinition(t, out StructInfo def)) { definition = def; return; }
			def.name = t.Name;
			var ps = t.GetProperties();
			var mp = typeof(MutableStruct).GetProperties();
			foreach (var p in ps) {
				if (mp.Any(m => m.Name == p.Name)) continue;

				t = p.PropertyType;
				var aa = p.GetCustomAttribute<ArrayInfoAttribute>();
				if (aa != null) {
					if (t.IsArray) t = t.GetElementType();
					if (!typeSizable(t)) continue;
					def.Add(t, p.Name, aa.dims[0]);
				} else if (typeSizable(t)) def.Add(t, p.Name);
			}
			definition = def;
		}

		/// <summary>Tells if size of variable could be determined.</summary>
		/// <param name="t"></param>
		/// <returns></returns>
		private bool typeSizable(Type t) {
			var size = -1;
			try { size = binary.Size.Of(t); } catch (Exception e) { if (e.GetType() == typeof(CrossRefferenceException)) throw e; }
			if (size < 0) return false;
			return true;
		}

		#region Stream
		/// <summary>Load struct bytes from stream at given stream positon.</summary>
		/// <param name="s">Stream to read from.</param>
		/// <param name="at">Stre</param>
		public MutableStruct Load(Stream s, long at) {
			_ad = at;
			if (s != stream) { stream = s; return this; }
			bytes.at(0).read.fromStream(s, at, size);
			return this;
		}

		/// <summary>Saves current struct bytes to specified stream.
		/// If <see cref="stream"/> object is not set or is not writable, an error will be thrown.</summary>
		/// <param name="at"></param>
		public void Save(long at) {
			if (_pS == null) throw new Exception("The struc has no parent stream.");
			if (!_pS.CanWrite) throw new Exception("Parent stream is not opened for read operation.");
			_ad = at;
			bytes.at(0).write.ToStream(_pS, at);
		}
		#endregion

		public object readValue(VariableInfo v) {
			if (!v.isArray)
				return bytes.read.at(v.offset).data(v.type);
			var a = Array.CreateInstance(v.type, v.dimesnions[0]);
			for (int i = 0; i < v.dimesnions[0]; i++)
				a.SetValue(bytes.read.at(v.offset + v.size * i)
					.data(v.type), i);
			return a;
		}

		private GCHandle h;
		/// <summary>Pins sturct and returns pointer for unmanaged memory.
		/// Call <see cref="free"/> method when you are done wit the sturct.
		/// TODO: source change lock (the source wont change if sturct don't change, so it the issue only for dynamically changed structs.)
		/// </summary>
		/// <returns></returns>
		public IntPtr getIntPtr() {
			if(h == default)
				h = GCHandle.Alloc(bytes.source, GCHandleType.Pinned); 
			return h.AddrOfPinnedObject();
		}
		public void free() { if (h != default) h.Free(); }

		#region Properties accessing
		/// <summary>Read variable from the struct raw bytes</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">Name of variable property.</param>
		/// <returns></returns>
		protected T read<T>([CallerMemberName] string name = null)
			=> bytes.read.at(definition.offsetOf(name)).data<T>();

		protected T readM<T>([CallerMemberName] string name = null)
			where T : MutableStruct
		{
			var m = Activator.CreateInstance<T>();
			m.bytes.write.at(0).bytes(
				bytes.at(definition.offsetOf(name)),
				m.size);
			return m;
		}

		/// <summary>Read array from the struct raw bytes</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">Name of variable property.</param>
		/// <returns></returns>
		protected T[] readArray<T>([CallerMemberName] string name = null) {
			var d = definition[name];
			var a = new T[d.totalLength];
			for (int i = 0; i < a.Length; i++)
				a[i] = bytes.read.at(d.offsetAt(i)).data<T>();
			return a;
		}

		/// <summary>Write variable to raw struct bytes</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">New value for the variable.</param>
		/// <param name="name">Name of varaible property.</param>
		protected void write<T>(T value, [CallerMemberName] string name = null)
			=> bytes.write.at(definition.offsetOf(name)).data(value);

		protected void writeM(MutableStruct value, [CallerMemberName] string name = null)
			=> bytes.write.at(definition.offsetOf(name))
			.bytes(value.bytes.at(0));

		/// <summary>Write variable to raw struct bytes</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">New value for the variable.</param>
		/// <param name="name">Name of varaible property.</param>
		protected void write<T>(T[] value, [CallerMemberName] string name = null) {
			var d = definition[name];
			var l = System.Math.Min(d.dimesnions[0], value.Length);
			for (int i = 0; i < l; i++)
				bytes.write.at(d.offsetAt(i)).data(value[i]);
		}
		#endregion

		public static implicit operator bool(MutableStruct s) => s != null;
	}

	public class array<T> : MutableStruct {
		public T[] raw {
			get => readArray<T>();
			set => write(value);
		}

		public array(int s, params int[] d) {
			raw = new T[s];
		}

		public T this[int i] => raw[i];

	}


	public class StructInfo : IEnumerable<VariableInfo> {
		private List<VariableInfo> _vars = new List<VariableInfo>();
		public int Count => _vars.Count;

		public string name { get; set; }
		/// <summary>Total size of the struct</summary>
		public int size { get; private set; } = 0;

		//public void Add<T>(T d, string name, int length = 0) where T : struct {
		//	Add<T>(name, length);
		//}

		//public void Add<T>(string name, int length = 0) where T : struct {
		//	VariableInfo v = (typeof(T), name, size, length > 0);
		//	_vars.Add(v);
		//	var s = Marshal.SizeOf<T>();
		//	size += v.isArray ? s * length : s;
		//}

		public void Add(Type t, string name, int length = 0) {
			var a = length > 0;
			var s = binary.Size.Of(t);
			VariableInfo v = new VariableInfo(t, name, s, size,
				a, a ? new int[] { length } : null);
			_vars.Add(v);
			s = a ? s * length : s;
			size += s;
		}


		/// <summary>Returns size of struct variable with given name.</summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public int offsetOf(string name) => _vars.Find(v => v.name == name).offset;
		/// <summary></summary>
		/// <param name="name"></param>
		/// <param name="i">Specifies index of the varaible (like if it is stored in an array).</param>
		/// <returns></returns>
		public int offsetOf(string name, int i) {
			var vr = _vars.Find(v => v.name == name);
			return vr.offset + vr.size * i;
		}

		public VariableInfo this[string name] => _vars.Find(v => v.name == name);
		/// <summary>Returns info about variable with given name.</summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public VariableInfo variable(string name) => this[name];
		public VariableInfo this[int i] => _vars[i];

		public IEnumerator<VariableInfo> GetEnumerator() => _vars.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _vars.GetEnumerator();

		public override string ToString() => $"{name} struct definition";

		public static implicit operator bool(StructInfo s) => s != null;
	}

	/// <summary>Provides information about single variable of a some struct.</summary>
	public class VariableInfo {
		/// <summary>Type of the variable.</summary>
		public Type type { get; private set; }
		/// <summary>Name of the variable.</summary>
		public string name { get; private set; }
		/// <summary>Size of variable in bytes.</summary>
		public int size { get; private set; }
		/// <summary>Total size of variable in bytes.</summary>
		public int totalSize { get; private set; }
		/// <summary>Total lengt of variable (number of items in all dimensions if variable is an array).</summary>
		public int totalLength { get; private set; }
		/// <summary>Offset of the variable relative to parent struct position.</summary>
		public int offset { get; private set; }
		/// <summary>Tells that this variable stores mulitple values in a row, of the type.</summary>
		public bool isArray { get; private set; }
		/// <summary>Stores lengths of all dimensions (if this variable <see cref="isArray"/>)</summary>
		public int[] dimesnions { get; private set; }

		public VariableInfo() { }
		public VariableInfo(Type type, string name, int size, int offset, bool isArray, int[] dims = null) {
			this.type = type;
			this.name = name;
			this.offset = offset;
			this.size = size;
			this.isArray = isArray;
			this.dimesnions = dims;

			if (!isArray) return;
			totalLength = dimesnions[0];
			for (int i = 1; i < dimesnions.Length; i++)
				totalLength *= dimesnions[i];
			totalSize = totalLength * size;
		}

		public int offsetAt(int i) => offset + size * i;

		public override string ToString() {
			var s = type.Name;
			if (isArray) {
				s += "[";
				foreach (var d in dimesnions) s += d.ToString() + ", ";
				s = s.Substring(0, s.Length - 2) + "]";
			}
			s += $" {name} (o:{offset} s:{size})";
			return s;
		}

		public static implicit operator VariableInfo((Type type, string name, int offset) v)
			=> new VariableInfo { type = v.type, name = v.name, offset = v.offset };
		public static implicit operator VariableInfo((Type type, string name, int offset, bool array) v)
			=> new VariableInfo { type = v.type, name = v.name, offset = v.offset, isArray = v.array };
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class ArrayInfoAttribute : Attribute {
		public int[] dims { get; private set; }

		public ArrayInfoAttribute(params int[] dimensions) {
			dims = dimensions;
		}
	}
}
