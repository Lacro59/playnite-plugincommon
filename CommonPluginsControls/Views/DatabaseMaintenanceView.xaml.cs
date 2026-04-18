using CommonPluginsControls.ViewModels;
using CommonPluginsShared.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Views
{
	public partial class DatabaseMaintenanceView : UserControl
	{
		public DatabaseMaintenanceView(IPluginDatabase pluginDatabase)
		{
			InitializeComponent();
			var vm = new DatabaseMaintenanceViewModel(pluginDatabase);
			vm.RequestClose += () => Window.GetWindow(this)?.Close();
			DataContext = vm;
		}
	}
}
