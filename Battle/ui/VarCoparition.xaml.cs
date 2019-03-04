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

namespace Battle.ui {
	/// <summary>
	/// Interaction logic for VarCoparition.xaml
	/// </summary>
	public partial class VarCoparition : UserControl {
		public VarCoparition() {
			InitializeComponent();
		}

		private string _vName;
		public string varName {
			get => _vName;
			set {
				_vName = value;
				aName.Content = _vName;
				bName.Content = _vName;
			}
		}
	}
}
