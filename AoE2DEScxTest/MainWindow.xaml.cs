using Battle.binary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AoE2DEScxTest {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			var ins = File.Open(@"C:\Users\Paweł\Games\Age of Empires 2 DE\76561198087738062\resources\_common\scenario\testScenario.aoe2scenario"
				, FileMode.OpenOrCreate);

			var h = new Header();
			h.Load(ins, 0);
			Debug.WriteLine(h.version);
			Debug.WriteLine(h.headerLen);
			Debug.WriteLine(h.savable);
			Debug.WriteLine(h.instructions);
			Debug.WriteLine(h.individualVictoriesUsed);
			Debug.WriteLine(h.playerCount);
		}
	}

	public class Header : MutableStruct {
		[StringInfo(4, "ASCII", true)]
		public string version {
			get => read<string>();
			set => write(value);
		}

		public uint headerLen {
			get => read<uint>();
			set => write(value);
		}

		public int savable {
			get => read<int>();
			set => write(value);
		}

		public uint savaTimeStamp {
			get => read<uint>();
			set => write(value);
		}

		[StringInfo(4, "ASCII")]
		public string instructions {
			get => read<string>();
			set => write(value);
		}

		public uint individualVictoriesUsed {
			get => read<uint>();
			set => write(value);
		}

		public uint playerCount {
			get => read<uint>();
			set => write(value);
		}
	}

}
