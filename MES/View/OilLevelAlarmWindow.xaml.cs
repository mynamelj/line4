using System.Windows;

namespace MES.View
{
    public partial class OilLevelAlarmWindow : Window
    {
        public OilLevelAlarmWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
