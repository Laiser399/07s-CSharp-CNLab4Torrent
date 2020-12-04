using CNLab4;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace CNLab4_Client.GUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowVM _viewModel;

        public MainWindow(int port)
        {
            InitializeComponent();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new IPEndPointConverter(),
                    new BitArrayConverter()
                }
            };

            General.PeerPort = port;

            General.OnNewLog += (_, args) =>
            {
                foreach (string line in args.Lines)
                {
                    LogTextBox.AppendText(line);
                    LogTextBox.AppendText("\n");
                }
                if (IsScrollToDown.IsChecked == true)
                    LogTextBox.ScrollToEnd();
            };

            _viewModel = new MainWindowVM(this);
            DataContext = _viewModel;

            
        }
    }
}
