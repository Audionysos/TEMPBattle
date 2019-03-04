using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Battle.coord {
	public class Subrange {

		public RectI global;
		public RectI basse;
		public RectI range;
		public PointI size => range.size;

		public Subrange(RectI global = null, RectI basse = null, RectI range = null) {
			this.global = global ?? new RectI((100, 100));
			this.basse = basse ?? this.global;
			this.range = range ?? this.basse.copy;
		}
		
		/// <summary>Translate local coordinates to basse coordinates.
		/// Null is returned if outpu coordinate is out of basse range.</summary>
		/// <param name="I"></param>
		/// <returns></returns>
		public PointI postion(PointI p) => basse.liesInside(range.position + p);


	}

	/// <summary>Subrange pointer. Allows to move within specific subrange.</summary>
	public class PtrSr {
		public Subrange range { get; private set; }
		/// <summary>Stores current positon of the pointer.</summary>
		private PointI _pos = new PointI();
		/// <summary>Returns current position of the pointer.</summary>
		public PointI position => _pos.copy;
		public int x => _pos.x;
		public int y => _pos.y;

		private Wrap _w = Wrap.AUTO | Wrap.HORIZONTAL | Wrap.AUTO_FLIP;  
		public Wrap wrap {
			get => _w;
			set {
				_w = value;
				if (!(_w ^ ~(Wrap.BOTH & Wrap.AUTO))) throw new Exception($"The wrap have to be set to at least on of {nameof(Wrap.HORIZONTAL)}, {nameof(Wrap.VERTICAL)} or {nameof(Wrap.AUTO)}"); 
				upgradeWrap();
				upgradeFlip();
			}
		}

		private void upgradeWrap() {
			if (_w & Wrap.AUTO == 0) return;
			_w = (_w | Wrap.BOTH) ^ Wrap.BOTH;
			var f = range.range.size.divide(_md);
			_w |= f.x > f.y ? Wrap.VERTICAL : Wrap.HORIZONTAL; 
		}

		/// <summary>The wrap walue should be up to date before calling this method.</summary>
		private void upgradeFlip() {
			if (_w ^ ~Wrap.AUTO_FLIP) return;
			var rs = range.range.size;
			if (_w & Wrap.HORIZONTAL && _pos.x > rs.x / 2 ||
				_w & Wrap.VERTICAL && _pos.y < rs.y)
				_w |= Wrap.FLIP;
			else _w = _w & Wrap.FLIP ^ Wrap.FLIP;
			_flip = _w & Wrap.FLIP ? 1 : 0;
		}

		private void upgradeDC()
			=> dc = (range.size - 0).copyIf(_md, c => c < 0);

		private int _flip = 1;
		public bool flipWrap {
			get => _flip > 0;
			set => _flip = value ? -1 : 1;
		}

		public bool firstRow => _pos.y == 0;
		public bool firstColumn => _pos.x == 0;

		private PointI dc;
		private PointI _md = PointI.right;
		public PointI moveDirection {
			get => _md;
			set {
				if (!value) throw new Exception("Move direction cannot be null.");
				_md = value;
				upgradeDC();
				upgradeWrap();
				upgradeFlip();
			}
		}

		public PtrSr next(int c = 1) {
			var r = range.range;
			//var xFist = _md.y == 0 || f.x < f.y;
			//var dc = (r.size-1).copyIf(_md, (s, m) => m < 0);
			var tv = _md * c;
			var div =  tv / (r.size - (dc - _pos).abs());

			if(_w & Wrap.HORIZONTAL) {
				tv.y += div.x; tv.y *= _flip;
				tv.x -= r.size.x * div.x;
			}
			if (_w & Wrap.VERTICAL) {
				tv.x += div.y; tv.x *= _flip;
				tv.y -= r.size.y * div.y;
			}

			_pos += tv; 
			return this;
		}

		public PtrSr previous(int c = 1) {
			_md = -_md; next(c); _md = -_md;
			return this;
		}

		private static PtrSr lastInstance;

		public PtrSr(Subrange r) { range = r; lastInstance = this; upgradeDC(); } 
		public PtrSr(Subrange r, PointI pos) {
			range = r;
			_pos = pos ?? new PointI();
			lastInstance = this;
			upgradeDC();
			upgradeWrap();
			upgradeFlip();
			if (!range.range.liesInside(_pos)) throw new ArgumentException("Specified point in not in range of givne subrange.");
		}

		public static PtrSr operator +(PtrSr p, int i) => p.next(i);
		public static Object operator ~(PtrSr p) => 10;

		public static PtrSr operator ++(PtrSr p) => p.next();
		public static PtrSr operator --(PtrSr p) => p.previous();

		public static implicit operator PtrSr(Subrange r) =>
			new PtrSr(r);

		public static implicit operator PtrSr(int i) =>
			new PtrSr(lastInstance.range, i);
		public static implicit operator PtrSr(PointI p) =>
			new PtrSr(lastInstance.range, p);

		public override string ToString() => position.ToString();

		public static implicit operator bool(PtrSr p) => p.range.range.liesInside(p.position);
	}

	public class Wrap {
		public static readonly Wrap HORIZONTAL = 1;
		public static readonly Wrap VERTICAL = 2;
		public static readonly Wrap BOTH = 3;
		public static readonly Wrap AUTO = 4;
		public static readonly Wrap FLIP = 8;
		public static readonly Wrap AUTO_FLIP = 16;

		public uint v { get; private set; }
		private Wrap(uint v) => this.v = v;

		public static implicit operator Wrap(uint i) => new Wrap(i);
		public static implicit operator uint(Wrap i) => i.v;
		public static Wrap operator |(Wrap w1, Wrap w2) => w1.v | w2.v;
		public static Wrap operator &(Wrap w1, Wrap w2) => w1.v & w2.v;
		public static Wrap operator ^(Wrap w1, Wrap w2) => w1.v ^ w2.v;
		public static implicit operator bool(Wrap w) => w.v > 0;

		public override string ToString() { var s = "";
			foreach (var f in GetType().GetFields(BindingFlags.Static | BindingFlags.Public)) {
				var v = f.GetValue(GetType()) as Wrap;
				if (v == null || (this & v) ^ v) continue;
				s += f.Name + ", ";
			}return s + $"({v})";
		}
	}

	public class PointI {
		#region Directions
		public static readonly PointI right = (1, 0);
		public static readonly PointI bottom_right = (1, 1);
		public static readonly PointI bottom = (0, 1);
		public static readonly PointI bottom_left = (-1, 1);
		public static readonly PointI left = (-1, 0);
		public static readonly PointI top_left = (-1, -1);
		public static readonly PointI top = (0, -1);
		public static readonly PointI top_right = (1, -1);
		#endregion

		public int x = 0;
		public int y = 0;

		public PointI() { }
		public PointI(int x = 0, int y = 0) {
			this.x = x; this.y = y;
		}

		/// <summary>Set signs (negative or positive values) of components to sings of corresponding components of given point and returns this point.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI signs(PointI p) {
			if (System.Math.Sign(x) != System.Math.Sign(p.x)) x *= -1;
			if (System.Math.Sign(y) != System.Math.Sign(p.y)) y *= -1;
			return this;
		}

		/// <summary>Set all components to absolute values and return this point.</summary>
		/// <returns></returns>
		public PointI abs() {
			x = System.Math.Abs(x);
			y = System.Math.Abs(y);
			return this;
		}

		#region compartions
		/// <summary>Retuns given point if any components are smaller or equal to corresponding components of this point, otherwise null.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI anyLes(PointI p) => p.x <= x || p.y <= y ? null : p;

		/// <summary>Retuns given point if all components are smaller or equal to corresponding components of this point, otherwise null.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI allLes(PointI p) => p.x <= x && p.y <= y ? null : p;

		/// <summary>Retuns given point if all components are smaller than corresponding components of this point, otherwise null.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI allOut(PointI p) => p && (p.x < x && p.y < y) ? p : null;

		/// <summary>Retuns given point if all its components are greater or equal to corresponding components of this point, otherwise null.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI allIn(PointI p) => p.x >= x && p.y >= y ? p : null;
		#endregion


		#region Conversion
		public static implicit operator PointI((int x, int y) t)
			=> new PointI(t.x, t.y);

		public static implicit operator PointI(int a)
			=> new PointI(a, a);
		#endregion

		#region Operations

		#region Addition
		public PointI add(PointI p) {
			x += p.x; y += p.y; return this;
		}

		public static PointI operator +(PointI p1, PointI p2)
			=> new PointI(p1.x + p2.x, p1.y + p2.y);
		#endregion

		#region Subtraction
		public static PointI operator -(PointI p1, PointI p2)
			=> new PointI(p1.x - p2.x, p1.y - p2.y);

		public static PointI operator -(PointI p1, int v)
			=> new PointI(p1.x - v, p1.y - v);
		
		/// <summary>Subtract components of another point for components of this point and return this point instance.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI sub(PointI p) {
			x -= p.x; y -= p.y; return this;
		}
		#endregion

		public static PointI operator *(PointI p1, int i)
			=> new PointI(p1.x * i, p1.y * i);

		public (double x, double y) divide(PointI o)
			=> ((double)x / o.x, (double)y / o.y);

		public static PointI operator /(PointI p1, PointI p2)
			=> new PointI(	p2.x == 0 ? int.MaxValue : p1.x / p2.x,
							p2.y == 0 ? int.MaxValue : p1.y / p2.y);

		/// <summary>Changes sing of each component of the point.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static PointI operator -(PointI p)
			=> new PointI(p.x * -1, p.y * -1);

		#region Copying
		public PointI copy => new PointI(x, y);
		/// <summary>Copy component to new point if it maches the criteria, otherwise use default value.</summary>
		/// <param name="ch"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public PointI copyIf(Func<int, bool> ch, int d = 0)
			=> new PointI(ch(x) ? x : d, ch(y) ? y : d);

		/// <summary>Copy component of this point to new point if it maches the criteria, otherwise use default value.
		/// Given function can compare components of this and other given point. Component of this point comes first.</summary>
		/// <param name="ch"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public PointI copyIf(PointI o, Func<int, int, bool> ch, int d = 0)
			=> new PointI(ch(x, o.x) ? x : d, ch(y, o.y) ? y : d);

		public PointI copyIf(PointI o, Func<int, bool> ch, int d = 0)
			=> new PointI(ch(o.x) ? x : d, ch(o.y) ? y : d);
		#endregion

		#endregion

		public override string ToString() => $"{x}:{y}";

		public static implicit operator bool(PointI p) => p!=null;
	}

	public class RectI {
		public PointI position;
		public PointI size;
		public PointI end => position + size;
		
		public RectI(PointI size = null, PointI position = null) {
			this.position = position ?? new PointI();
			this.size = size ?? new PointI();
		}

		/// <summary>Returns given point only if it lies inside this rect or on it's edge.</summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public PointI liesInside(PointI p) => end.allOut(position.allIn(p));


		public RectI copy => new RectI(size.copy, position.copy);

		public static implicit operator RectI((int x, int y) t) => new RectI(t);
	}

}
