using Battle.binary;
using System;
using System.Collections.Generic;
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

namespace Battle.ui
{
    /// <summary>
    /// Interaction logic for BytesView.xaml
    /// </summary>
    public partial class BytesView : UserControl
    {
		private Brush _columnBG2 = Brushes.PaleTurquoise;
		public Brush columnBG2 {
			get => _columnBG2;
			set {
				_columnBG2 = value;
				colBGs.ForEach(b => b.Background = _columnBG2);
			}
		}
		private Brush _columnBG3 = Brushes.LightBlue;
		public Brush columnBG3 {
			get => _columnBG3;
			set {
				_columnBG3 = value;
				vPC.Background = _columnBG3;
				hPC.Background = _columnBG3;
			}
		}
		private List<Border> colBGs = new List<Border>();

		/// <summary>Vertical positon column bacground.</summary>
		private Border vPC;
		/// <summary>Horizontal positon column bacground.</summary>
		private Border hPC;

		private List<TextBox> byTexts = new List<TextBox>(); 
        public BytesView()
        {
            InitializeComponent();
			createGrid();
        }

		private (int X, int Y) resStart = (1, 1);
		private (int X, int Y) resEnd = (0, 0);
		private int tCol => xs + resStart.X + resEnd.X;

		private int xs = 8;
		private int ys = 8;
		private int cellSize = 1;
		private Thickness borderThickness = new Thickness(0);
		private void createGrid() {
			createColumns();

			//columns backgrounds
			for (int x = resStart.X; x < xs + tCol; x++) {
				if ((x - resStart.X) % 2 == 0) continue;
				var b = setColumnBG(x);
				colBGs.Add(b);
			}

			vPC = setColumnBG(0); //position column bg
			vPC.Background = Brushes.LightBlue;

			hPC = setRowBG(0);
			hPC.Background = Brushes.LightBlue;

			//text cells
			for (int y = 0; y < ys + resStart.Y; y++) {
				for (int x = 0; x < xs + resStart.X; x++) {
					var tb = new TextBox();
					tb.Background = null;
					Grid.SetColumn(tb, x);
					Grid.SetRow(tb, y);
					tb.Text = $"{x},{y}";
					tb.Padding = new Thickness(2);
					tb.HorizontalAlignment = HorizontalAlignment.Center;
					tb.VerticalAlignment = VerticalAlignment.Center;
					tb.BorderThickness = borderThickness;
					main.Children.Add(tb);
					byTexts.Add(tb);
				}
			}

			
			for (int i = resStart.Y; i < ys + resStart.Y; i++) {
				textAt(0, i).Text = (xs * i).ToString("00000000");
			}

			var hps = new GridSplitter();
			hps.Background = Brushes.Black;
			hps.Width = 1;
			Grid.SetRowSpan(hps, int.MaxValue);
			Grid.SetColumn(hps, 0);
			main.Children.Add(hps);

			var vps = new GridSplitter();
			vps.Background = Brushes.Black;
			vps.HorizontalAlignment = HorizontalAlignment.Stretch;
			vps.VerticalAlignment = VerticalAlignment.Bottom;
			vps.Height = 1;
			Grid.SetColumnSpan(vps, int.MaxValue);
			Grid.SetRow(vps, 0);
			main.Children.Add(vps);
		}

		private void createColumns() {
			for (int x = 0; x < xs + resStart.X; x++) {
				var cd = new ColumnDefinition();
				cd.Width = new GridLength(1, GridUnitType.Auto);
				main.ColumnDefinitions.Add(cd);
			}

			for (int y = 0; y < ys + resStart.Y; y++) {
				var rd = new RowDefinition();
				rd.Height = new GridLength(1, GridUnitType.Auto);
				main.RowDefinitions.Add(rd);
			}
		}

		private Border setColumnBG(int x, int y = 0) {
			var b = new Border() {
				Background = columnBG2,
			};
			Grid.SetColumn(b, x);
			Grid.SetRow(b, y);
			Grid.SetRowSpan(b, int.MaxValue);
			main.Children.Add(b);
			return b;
		}

		private Border setRowBG(int y, int x = 0) {
			var b = new Border() {
				Background = columnBG2,
			};
			Grid.SetColumn(b, x);
			Grid.SetRow(b, y);
			Grid.SetColumnSpan(b, int.MaxValue);
			main.Children.Add(b);
			return b;
		}

		public TextBox textAt(int x, int y)
			=> byTexts[tCol * y + x];

		public TextBox byteTextAt(int x, int y)
			=> byTexts[resStart.Y * tCol + y * tCol + resStart.X + x];

		public Bytes _bs;
		public Bytes bytes {
			get => _bs;
			set {
				_bs = value;
				for (int y = 0; y < ys; y++) {
					for (int x = 0; x < xs; x++) {
						var bt = byteTextAt(x, y);
						bt.Text = _bs.source[xs * y + x].ToString("X2");
					}
				}
			}
		}
	}

	
}
