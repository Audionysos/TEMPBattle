using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using static System.BitConverter;
using static Simple.Size;

namespace Simple {
	public partial class MainWindow : Window {

		private StrRead[] strs = new StrRead[] {
			new StrRead() { //math
				size    = Int* 11 + ChaR* 4 + DoublE* 10 + Float* 1,
				tabPos  = Int* 9  + ChaR* 4              + Float* 1,
				addSize = Int* 9  + ChaR* 4 + DoublE* 10 + Float* 1,
				addPos  = Int* 10 + ChaR* 4 + DoublE* 10 + Float* 1,
				process = (bs, p, i) => bs.write(GetBytes(ToDouble(bs, p) + i), p),
				streamProcess = (ins, os, i) => os.Write(ins.ReadDouble() + i),
				tiSize = DoublE,
			},
			new StrRead() { //alphabet
				size    = Int* 4 + ChaR* 12 + DoublE* 2 + Float* 11,
				tabPos  = Int* 2 + ChaR* 2              + Float* 11,
				addSize = Int* 2 + ChaR* 12 + DoublE* 2 + Float* 11,
				addPos  = Int* 3 + ChaR* 12 + DoublE* 2 + Float* 11,
				process = (bs, p, i) => bs.write(GetBytes((char)(ToChar(bs, p) + 1)), p),
				streamProcess = (ins, os, i) => os.Write((char)(ins.ReadChar() + 1)),
				tiSize = ChaR,
			},
			new StrRead() { //parametr
				size    = Int* 67 + ChaR* 9 + DoublE* 1 + Float* 1,
				tabPos  = Int* 5  + ChaR* 9 + DoublE* 1 + Float* 1,
				addSize = Int* 65 + ChaR* 9 + DoublE* 1 + Float* 1,
				addPos  = Int* 66 + ChaR* 9 + DoublE* 1 + Float* 1,
				process = (bs, p, i) => {
					for (int j = 0; j < 6; j++)
						bs.write(GetBytes(ToInt32(bs, p+j) % 100), p);
				},
				streamProcess = (ins, os, i) => {
					for (int j = 0; j < 6; j++)
						os.Write(ins.ReadInt32() % 100);
				},
				tiSize = Int,
			}
		};

		private string inputFile = "test";
		private string outputFile = "test_wynik";

		public MainWindow() {
			InitializeComponent();
			loaded();
			
		}

		private void loaded() {
			var ibs = File.ReadAllBytes(inputFile);
			var p = 0; int c; var sts = 10; //sturct postion, code, and standard tab size
			do {
				var s = strs[c = ToInt32(ibs, p)];
				var lp = p + s.tabPos;
				var ic = ToInt32(ibs, p + s.addSize) * s.tiSize + sts;
				for (int i = 0; i < ic; i++) {
					if (i == sts) lp = ToInt32(ibs, p + s.addPos);
					s.process(ibs, lp++, i);
				}
				p += s.size;
			} while (c >= 0 && c <= 2);
			File.WriteAllBytes(outputFile, ibs);
		}

		private void streamed(int start, int count = int.MaxValue) {
			BinaryReader sr = new BinaryReader(File.OpenRead(inputFile));
			BinaryWriter sw = new BinaryWriter(File.OpenWrite(outputFile));
			var p = start; int c; var sts = 10; //sturct postion, code, and standard tab size
			sr.at(p); sw.at(p);
			var sc = 0;
			do {
				sc++;
				var s = strs[c = sr.at(p).ReadInt32()];
				var ic = sr.at(p+s.addSize).ReadInt32() * s.tiSize + sts;
				sw.BaseStream.Position = sr.BaseStream.Position = p + s.tabPos;
				for (int i = 0; i < ic; i++) {
					if (i == sts)
						sw.BaseStream.Position = sr.BaseStream.Position
							= sr.at(p + s.addPos).ReadInt32();
					s.streamProcess(sr, sw, i);
				}
				p += s.size;
			} while (sc == 0 || (c >= 0 && c <= 2));
			sr.Close(); sw.Close();
		}

		/// <summary>Processing units count per thread</summary>
		private int pcpt = 500_000;
		private void multiStream() {
			BinaryReader sr = new BinaryReader(File.OpenRead(inputFile));
			var p = 0; int c;//sturct postion, code
			var sc = 0; var pc = 0; //struct and processing count
			List<Thread> ts = new List<Thread>(100);
			do {
				sc++;
				var s = strs[c = sr.at(p).ReadInt32()];
				pc += 11 + sr.at(p + s.addSize).ReadInt32() / s.tiSize;
				if (pc > pcpt) {
					var t = new Thread(() => {
						streamed(p, sc);
					});
					ts.Add(t);
					pc = sc = 0;
				}
				p += s.size;
			} while (c >= 0 && c <= 2);
			for (int i = 0; i < ts.Count; i++)
				ts[i].Join();
		}

		private class StrRead {
			public int code;
			public int size;
			public Action<object> vars;
			public int tabPos;
			public int addSize;
			public int addPos;
			public int tiSize;
			public Action<byte[], int, int> process;
			public Action<BinaryReader, BinaryWriter, int> streamProcess;
		}
	}

	internal static class BArrExt {
		public static void write(this byte[] d, byte[] s, int p)
			=> Buffer.BlockCopy(s, 0, d, p, s.Length);
		public static BinaryReader at(this BinaryReader r, int p) {
			r.BaseStream.Position = p; return r;
		}
		public static BinaryWriter at(this BinaryWriter r, int p) {
			r.BaseStream.Position = p; return r;
		}
	}

	public static class Size {
		public static readonly int ChaR = sizeof(char);
		public static readonly int Int = sizeof(int);
		public static readonly int DoublE = sizeof(double);
		public static readonly int Float = sizeof(float);
	}
}
