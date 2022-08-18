using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
		
		/// <summary>Size of the struct in bytes.
		/// In case of structs of dynamic size, reading this property will actually read dynamic part of the struct to determine actual size.
		/// To you want to know size of static size, see <see cref="StructInfo.size"/> property of struct's <see cref="definition"/>.</summary>
		public int size {
			get {
				if(definition.constSize) return definition.size;
				sizeInfo = sizeInfo ?? new SizeInfo(this);
				return sizeInfo.size;
			}
		}
		/// <summary>Object providing information about sturcture size (used only if structure has dynamic size).</summary>
		public SizeInfo sizeInfo { get; private set; }

		#region Stream
		/// <summary>Parent stream.</summary>
		private Stream _pS;
		/// <summary>Parent stream from where the struct was/will be loaded.
		/// If sturuct was loaded already, it will be realoaded at the same position in new stream.</summary>
		public Stream stream {
			get => _pS;
			set {
				_pS = value;
				if (_pS == null) { _ad = -1; return; }
				if (_pS.CanRead && _ad >= 0) Load(_pS, _ad);
			}
		}

		/// <summary>Loads additonal bytes of the sturct from the stream and updates <see cref="sizeInfo"/> for additional size.</summary>
		/// <param name="bytesCount"></param>
		internal void loadNext(int bytesCount) {
			if (definition.constSize) throw new InvalidOperationException("Structs of const size cannot load additional bytes, once they loaded.");
			bytes.grow(bytesCount);
			var ns = bytes.source.Length - bytesCount; //start of new bytes
			var pp = bytes.position;
			bytes.read.at(ns).fromStream(stream, _ad + ns, bytesCount);
			bytes.position = pp;
			sizeInfo = sizeInfo ?? new SizeInfo(this);
			sizeInfo._size += bytesCount;
		}

		#endregion

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
				}else {
					var sa = p.GetCustomAttribute<StringInfoAttribute>();
					if(sa!= null) {
						def.Add(t, p.Name, 0, sa.size, sa.constSize, new StringConverter(sa));
					}else if (typeSizable(t)) def.Add(t, p.Name);
				} 
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
			bytes.at(0).read.fromStream(s, at, definition.size);
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

		#region Extras
		public object readValue(VariableInfo v) {
			if (!v.isArray) {
				if (v.converter) {
					bytes.position = v.offset;
					return v.converter.read(bytes, new VariableConversionContext(this, v));
				} else return bytes.read.at(v.offset).data(v.type);
			}
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
		#endregion

		#region Properties accessing

		/// <summary>Read variable from the struct raw bytes</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">Name of variable property.</param>
		/// <returns></returns>
		protected T read<T>([CallerMemberName] string name = null) {
			var d = definition[name];
			if (d.converter) {
				bytes.position = d.offset;
				return (T)d.converter.read(bytes, new VariableConversionContext(this, d)); //TODO: The variables conversion contexts could be pooled 
			}else return bytes.read.at(definition.offsetOf(name)).data<T>();
		}

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

		#region Writing 
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

		#endregion

		public static implicit operator bool(MutableStruct s) => s != null;
	}

	/// <summary>Class for keeping track of particular dynmic size <see cref="MutableStruct"/>.</summary>
	public class SizeInfo {
		/// <summary>Index of variable info in <see cref="StructInfo"/> of the struct that is been tracked for which the current size was determined..</summary>
		internal int lastKnownSize = -1;
		internal int _size;
		public int size {
			get {
				var d = str.definition;
				for (int i = System.Math.Max(lastKnownSize, 0); i < d.Count; i++) {
					var v = d[i];
					if (!v.constSize) str.readValue(v); //TODO:this sould update the size
					lastKnownSize = i;
				}
				return _size;
			}
		}
		private MutableStruct str;

		public SizeInfo(MutableStruct mutableStruct) {
			this.str = mutableStruct;
			_size = mutableStruct.definition.size;
		}
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
		/// <summary>Number of variables this structure contains.</summary>
		public int Count => _vars.Count;

		public string name { get; set; }
		/// <summary>Total, know size of the struct.
		/// If sturct is not <see cref="constSize"/>, this will be size of all const-size varaibles, plus the minimul load size of dynamic variable.</summary>
		public int size { get; private set; } = 0;
		/// <summary>Tell is struct has constant size that is determined before loading it from memory.</summary>
		public bool constSize { get; private set; } = true;

		//public void Add<T>(T d, string name, int length = 0) where T : struct {
		//	Add<T>(name, length);
		//}

		//public void Add<T>(string name, int length = 0) where T : struct {
		//	VariableInfo v = (typeof(T), name, size, length > 0);
		//	_vars.Add(v);
		//	var s = Marshal.SizeOf<T>();
		//	size += v.isArray ? s * length : s;
		//}

		/// <summary>Adds new property in structure definition.</summary>
		/// <param name="t">Type of the property value.</param>
		/// <param name="name">name of the property</param>
		/// <param name="length">Length of the property (array length - 0 if normal property).</param>
		/// <param name="s">Size of the property value in bytes.
		/// If no size given (-1) the size is acquired from <see cref="binary.Size.Of(Type)"/> method.
		/// //If size is less then 0, it means the size of the varaible(and thus entire struct) cannot be determined until reading the strucuct.
		/// If <paramref name="constSize"/> parameter is false, the size is minimal length of bytes that need to be loaded after previous variable in order to be able to determine size of this variable.
		/// </param>
		/// <param name="constSize">Tells if varaible has a const size or it's size could be only determined at read time.</param>
		///<param name="c">Custom converter that is used to write and read bytes of the variable.</param>
		public void Add(Type t, string name, int length = 0, int s = -1, bool constSize = true, CustomVariableConverter c = null) {
			var a = length > 0;
			if(s == -1) s = binary.Size.Of(t);
			VariableInfo v = new VariableInfo(t, name,
				size: constSize ? s : -s,
				constSize:constSize,
				offset:this.constSize ? size : -1,
				a, a ? new int[] { length } : null);
			v.index = _vars.Count;
			_vars.Add(v);
			v.converter = c;
			if (!constSize) this.constSize = false;
			if (s < 0) { Debugger.Break(); s = 0; }
			s = a ? s * length : s;
			size += s;
		}

		#region Utils
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
		#endregion
	}

	/// <summary>Provides information about single variable of a some struct.</summary>
	public class VariableInfo {
		/// <summary>Index of the variable in parent struct.</summary>
		public int index;
		/// <summary>Type of the variable.</summary>
		public Type type { get; private set; }
		/// <summary>Name of the variable.</summary>
		public string name { get; private set; }
		/// <summary>Size of variable in bytes.</summary>
		public int size { get; private set; }
		/// <summary>Tells if this variable is has constat size of bytes.</summary>
		public bool constSize { get; private set; }
		/// <summary>Total size of variable in bytes.</summary>
		public int totalSize { get; private set; }
		/// <summary>Total lengt of variable (number of items in all dimensions if variable is an array).</summary>
		public int totalLength { get; private set; }
		//TODO: Get offset for post-dynamic variables
		/// <summary>Offset of the variable relative to parent struct position.
		/// If the offset value is negative, this means the stuct has dynamic size, and position of the varaible cannot be determinined unitl reading previous ones.</summary>
		public int offset { get; private set; }
		/// <summary>Tells that this variable stores mulitple values in a row, of the type.</summary>
		public bool isArray { get; private set; }
		/// <summary>Stores lengths of all dimensions (if this variable <see cref="isArray"/>)</summary>
		public int[] dimesnions { get; private set; }
		public CustomVariableConverter converter { get; set; }

		public VariableInfo() { }
		public VariableInfo(Type type, string name, int size, bool constSize, int offset, bool isArray, int[] dims = null) {
			this.type = type;
			this.name = name;
			this.offset = offset;
			this.size = size;
			this.isArray = isArray;
			this.dimesnions = dims;
			this.constSize = constSize;

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

	public abstract class CustomVariableConverter {

		/// <summary>Read object from given bytes at thier current position.</summary>
		public abstract object read(Bytes b, VariableConversionContext cc);
		/// <summary>Write object to given bytes at thier current position.</summary>
		public abstract void write(Bytes b, object value, VariableConversionContext cc);

		/// <summary>False if null.</summary>
		public static implicit operator bool(CustomVariableConverter c) => c!=null;
	}

	public class VariableConversionContext {
		public MutableStruct struc { get; }
		public VariableInfo variable { get; }

		public VariableConversionContext(MutableStruct str, VariableInfo v) {
			this.struc = str;
			this.variable = v;
		}

		/// <summary>Instructs the sturct to load additional bytes from it's source stream.</summary>
		public void loadBytes(int count) {
			struc.loadNext(count);
			struc.sizeInfo.lastKnownSize = variable.index;
		}

	}

	/// <summary>Handles reading and writing strings in <see cref="MutableStruct"/>.</summary>
	public class StringConverter : CustomVariableConverter {
		public StringInfoAttribute infoAtt { get; private set; }

		public StringConverter(StringInfoAttribute a) {
			this.infoAtt = a;
		}

		//TODO: need to grow source bytes after reading the size (or even before...)
		public override object read(Bytes b, VariableConversionContext cc) {
			var s = infoAtt.constSize ? infoAtt.size : BitConverter
				.ToInt32(b.read.bytes(infoAtt.size), 0);
			if (!infoAtt.constSize) {
				//var abc = s - System.Math.Abs(cc.variable.size); //additional bytes count (accounting initial pre-load size)
				cc.loadBytes(s);
			}return infoAtt.encoding.GetString(b.read.bytes(s));
		}

		public override void write(Bytes b, object value, VariableConversionContext cc) {
			var v = value?.ToString() ?? "";
			var bs = infoAtt.encoding.GetBytes(v);
			if (!infoAtt.constSize) {
				var l = BitConverter.GetBytes(bs.Length);
				b.write.bytes(l);
			}
			b.write.bytes(bs);
		}
	}

	#region Variables attributes
	[AttributeUsage(AttributeTargets.Property)]
	public class ArrayInfoAttribute : Attribute {
		public int[] dims { get; private set; }

		/// <summary></summary>
		/// <param name="dimensions">Specifies lengths for each dimension of the array.</param>
		public ArrayInfoAttribute(params int[] dimensions) {
			dims = dimensions;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class StringInfoAttribute : Attribute {
		public int size { get; }
		public Encoding encoding { get; }
		public bool constSize { get; }

		/// <summary></summary>
		/// <param name="size">Size of the string(if <paramref name="constSize"/> is true), or size of variable storing size of the string (in bytes).</param>
		/// <param name="encoding">Encoding of the string (see <see cref="Encoding.GetEncoding(string)"/> method that is used to assign <see cref="StringInfoAttribute.encoding"/> property).</param>
		/// <param name="constSize"></param>
		public StringInfoAttribute(int size, string encoding, bool constSize = false) {
			this.size = size;
			this.encoding = Encoding.GetEncoding(encoding);
			this.constSize = constSize;
		}

	}
	#endregion

}
