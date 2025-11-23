using System;
using System.Windows;

namespace ScriptWriter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                MessageBox.Show($"CRASH: {args.ExceptionObject}", "Unhandled Exception");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"CRASH: {args.Exception}", "Dispatcher Exception");
                args.Handled = false;
            };

            base.OnStartup(e);
        }
    }
}
