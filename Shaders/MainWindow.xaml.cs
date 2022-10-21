using adns.processing;
using System.Windows;

namespace Shaders {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			var osq = new ObjectSquaresView();
			this.Content = osq;
		}
	}
}
