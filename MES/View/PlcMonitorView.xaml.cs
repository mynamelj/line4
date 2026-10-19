using MES.ViewModel;
using System.Windows;

namespace MES.View
{
    public partial class PlcMonitorView : Window
    {
        public PlcMonitorView()
        {
            InitializeComponent();
            var viewModel = new PlcMonitorViewModel();
            DataContext = viewModel;
            Loaded += async (sender, args) => await viewModel.StartAsync();
            Closed += (sender, args) =>
            {
                viewModel.Dispose();
            };
        }
    }
}
