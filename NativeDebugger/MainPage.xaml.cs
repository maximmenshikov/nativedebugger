using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using System.Runtime.InteropServices;
using Microsoft.Phone.InteropServices;
using InteropSvc;
using Microsoft.Phone.Shell;
namespace WmdcLauncher
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool IsRunning = false;
        bool shownOnce = false;

        public MainPage()
        {
            InitializeComponent();
            AddToLog("<NO>");
            this.ViewModel.StateChanged += new EventHandler<MainViewModel.StateChangedEventArgs>(ViewModel_StateChanged);
        }

        void ViewModel_StateChanged(object sender, MainViewModel.StateChangedEventArgs e)
        {
            if (e.Type == MainViewModel.StateChangedType.IndicatorShow)
            {
                if (SystemTray.ProgressIndicator == null)
                    SystemTray.ProgressIndicator = new ProgressIndicator();
                SystemTray.ProgressIndicator.IsIndeterminate = e.ShowIndicator;
                SystemTray.ProgressIndicator.IsVisible = e.ShowIndicator;
                btnRun.IsEnabled = !e.ShowIndicator;
            }
            else if (e.Type == MainViewModel.StateChangedType.Logging)
            {
                AddToLog(e.Text);
            }
        }
        void AddToLog(string text)
        {
            if (text == "<NO>")
                txtLog.Text = "";
            else
                txtLog.Text = txtLog.Text + text;
        }

        public MainViewModel ViewModel
        {
            get
            {
                return this.DataContext as MainViewModel;
            }
        }

        void SetProgressBarAnimation(bool enabled)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                if (enabled)
                    ProgressBarEnableAnimation.Begin();
                else
                    ProgressBarDisableAnimation.Begin();
            }
            );
        }

        void MyRunApp()
        {
            ViewModel.Run();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            StartupAnimation.Begin();
            if (shownOnce == false)
            {
                shownOnce = true;
                AddToLog("<NO>");
                bool lr = InteropSvc.InteropLib.Initialize();
                if (InteropSvc.InteropLib.Instance == null || InteropSvc.InteropLib.Instance.HasRootAccess() == false)
                {
                    MessageBox.Show(LocalizedResources.NoRootAccess, LocalizedResources.Error, MessageBoxButton.OK);
                    throw new Exception("Quit");
                }

                ViewModel.RefreshState();
                ViewModel.RefreshDebuggerState();
                
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            MyRunApp();
        }

        private void txtAbout_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDisableDebuggers_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisableDebuggers();
        }

        private void btnEnableIntegration_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SystemIntegrationEnabled = !ViewModel.SystemIntegrationEnabled;
        }


    }

}