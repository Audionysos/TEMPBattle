using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Battle.coord {

	public class SubrangeTest {

		private List<List<TextBox>> texts = new List<List<TextBox>>();

		public SubrangeTest() {
			var g = new Grid();
			var r = new Subrange((5,5));

			//PtrSr p = new PtrSr(r, r.range.size-(1,1));
			PtrSr p = r;
			//p.moveDirection = PointI.right;
			//p.wrap = Wrap.HORIZONTAL;
			do {
				if (p.firstRow) {
					g.ColumnDefinitions.Add(new ColumnDefinition() { Name = $"c{p.x}" });
					texts.Add(new List<TextBox>());
				}
				if (p.firstColumn) g.RowDefinitions.Add(new RowDefinition() { Name = $"r{p.y}" });
				Debug.WriteLine($"{p.x}:{p.y}");
				var tb = new TextBox();
				tb.HorizontalContentAlignment = HorizontalAlignment.Center;
				tb.VerticalContentAlignment = VerticalAlignment.Center;
				tb.Text = p.ToString();
				Grid.SetColumn(tb, p.x);
				Grid.SetRow(tb, p.y);
				g.Children.Add(tb);
				texts[p.x].Add(tb);

			} while (p++);

			p = 0;
			do { texts[p.x][p.y].Text = "";
			} while (p++);

			var c = 0;
			Debug.WriteLine("VALUE:" + ~p);
			//p = r.range.size - (r.range.size.x, 1);
			//p = r.range.size - (1, r.range.size.y);
			p = 0;
			//p.wrap = Wrap.HORIZONTAL;
			//p.flipWrap = false;
			//p.moveDirection = PointI.bottom;
			p.moveDirection = (2,1);
			p.wrap.ToString();
			do {
				Debug.WriteLine($"{p.x}:{p.y}");
				texts[p.x][p.y].Text = ""+c++;
				//if(c%2==0) p.moveDirection += (0, -1);
			} while (p++);

			Application.Current.MainWindow.Content = g;
		}
	}
}
