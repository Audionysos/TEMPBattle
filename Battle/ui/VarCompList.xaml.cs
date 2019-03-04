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

namespace Battle.ui {
	/// <summary>
	/// Interaction logic for VarCompList.xaml
	/// </summary>
	public partial class VarCompList : UserControl {
		private List<VarCoparition> comps = new List<VarCoparition>();

		public VarCompList() {
			InitializeComponent();
		}

		private StructInfo _sInfo;
		public StructInfo structInfo {
			get => _sInfo;
			set {
				_sInfo = value;
				stack.Children.Clear();
				while (comps.Count < _sInfo.Count)
					comps.Add(new VarCoparition());
				for (int i = 0; i < _sInfo.Count; i++) {
					var v = _sInfo[i]; var cv = comps[i];
					cv.aName.Content = cv.bName.Content = v.name;
					stack.Children.Add(cv);
				}
				readStructs();
			}
		}

		private void readStructs() {
			for (int i = 0; i < _sInfo.Count; i++) {
				var v = _sInfo[i]; var cv = comps[i];
				if(_sA && _sA.definition == _sInfo)
					cv.aVal.Text = _sA.readValue(v).ToString();
				if(_sB && _sB.definition == _sInfo)
					cv.bVal.Text = _sB.readValue(v).ToString();
			}

		}

		private MutableStruct _sA; 
		public MutableStruct sturctA {
			get => _sA;
			set {
				_sA = value;
				readStructs();
			}
		}

		private MutableStruct _sB;
		public MutableStruct sturctB {
			get => _sB;
			set {
				_sB = value;
				readStructs();
			}
		}
	}
}
