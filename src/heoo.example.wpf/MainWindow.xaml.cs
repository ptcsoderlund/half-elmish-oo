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
        private ExampleApp.MyVm Vm => this.DataContext as ExampleApp.MyVm;
        public MainWindow()
        {
            InitializeComponent();
            //Step 3.1
            ExampleApp.program.OnModelUpdated = FSharpOption<Action<ExampleApp.Model>>.Some(onNewModel);
        }
        //Also step 3.1
        void onNewModel(ExampleApp.Model model)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(() => onNewModel(model));
            else Vm.updateModel(model); 
        }
        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            //Probably not necessary since everything else dies. 
            (ExampleApp.program as IDisposable)?.Dispose();
        }

        private void UpdateGetSetSomeText(object sender, TextChangedEventArgs e)
        {
            //wpf twoway binding spamreads the getter. Which will set textbox value before its updated.
            //We are using async code here which means the getter isnt uppdated.
            //In oneway binding, the getter is called when value is changed (onpropertychanged)
            //Which means we can work around this problem by using onewaymode and call the setter explicitly from here.
            Vm.GetSetSomeText = ((TextBox)sender).Text;
        }
    }
}