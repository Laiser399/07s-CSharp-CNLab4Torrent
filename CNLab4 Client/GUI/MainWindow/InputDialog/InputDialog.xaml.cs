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
using System.Windows.Shapes;

namespace CNLab4_Client.GUI
{
    /// <summary>
    /// Логика взаимодействия для InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public string TitleText
        {
            get => _viewModel.TitleText;
            set => _viewModel.TitleText = value;
        }

        public string InputText
        {
            get => _viewModel.InputText;
            set => _viewModel.InputText = value;
        }

        public Func<string, bool> InputValidator;

        private InputDialogVM _viewModel = new InputDialogVM();


        public InputDialog()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (InputValidator is null)
                DialogResult = true;
            else
            {
                bool isValid = InputValidator(InputText);
                if (isValid)
                    DialogResult = true;
                else
                    MessageBox.Show("Wrong input format.");
            }
        }
    }
}
