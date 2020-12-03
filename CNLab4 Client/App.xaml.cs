using CNLab4;
using CNLab4_Client.GUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CNLab4_Client
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var window = new MainWindow(59001);
            window.Show();
            window.Top = 340;
            window.Left = 100;


            //var dialog = new InputDialog
            //{
            //    TitleText = "Enter port:",
            //    InputText = "59001",
            //    InputValidator = (value) => int.TryParse(value, out _)
            //};

            //if (dialog.ShowDialog() == true)
            //{
                
            //}
            //else
            //{
            //    base.OnStartup(e);
            //}
        }
    }
}
