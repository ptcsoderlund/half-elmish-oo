using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using heoo.example.code;
using Microsoft.FSharp.Core;

namespace heoo.example.wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ExampleApp.ExampleVM Vm => this.DataContext as ExampleApp.ExampleVM;
        public MainWindow()
        {
            InitializeComponent();
            ExampleApp.ElmishProgram.OnModelUpdated = FSharpOption<Action<ExampleApp.Model>>.Some(onNewModel);
        }

        void onNewModel(ExampleApp.Model model)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(() => onNewModel(model));
            else ExampleApp.SingletonVm.updateModel(model);
        }
        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            //Probably not necessary since everything else dies. 
            (ExampleApp.ElmishProgram as IDisposable)?.Dispose();
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Vm.GetSetSomeText2 = ((TextBox)sender).Text;
        }
    }
}