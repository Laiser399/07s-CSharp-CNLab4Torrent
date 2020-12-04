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
        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = new MainWindow(59001);
            MainWindow.Show();
            MainWindow.Top = 80;
            MainWindow.Left = 100;
            MainWindow.Closed += (_, __) => Shutdown();

            //var dialog = new InputDialog
            //{
            //    TitleText = "Enter port:",
            //    InputText = "59001",
            //    InputValidator = (value) => int.TryParse(value, out _)
            //};

            //if (dialog.ShowDialog() == true)
            //{
            //    MainWindow = new MainWindow(int.Parse(dialog.InputText));
            //    MainWindow.Show();
            //    MainWindow.Top = 340;
            //    MainWindow.Left = 100;
            //    MainWindow.Closed += (_, __) => Shutdown();
            //}
            //else
            //{
            //    Shutdown();
            //}
        }
    }
}
