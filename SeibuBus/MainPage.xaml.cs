using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SeibuBusLib;

namespace SeibuBus
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool clickFlag = false;

        // コンストラクター
        public MainPage()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (clickFlag)
                return;
            if (!Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("ネットワークに接続していません", "エラー", MessageBoxButton.OK);
                return;
            }
            if (textBox1.Text.Length == 0 || textBox2.Text.Length == 0)
            {
                MessageBox.Show("停留所名を入力してください", "エラー", MessageBoxButton.OK);
                return;
            }
            ShowStatusBar("検索中です...", true);
            textBox1.IsReadOnly = true;
            textBox2.IsReadOnly = true;
            clickFlag = true;

            Location busObj = new Location();
            busObj.SearchRoute(textBox1.Text, textBox2.Text, delegate(List<BusInfo> bus){
                PhoneApplicationService.Current.State["Route"] = textBox1.Text + "→" + textBox2.Text;
                PhoneApplicationService.Current.State["Arr"] = textBox2.Text;
                PhoneApplicationService.Current.State["Dep"] = textBox1.Text;
                PhoneApplicationService.Current.State["Info"] = bus;
                ShowStatusBar(false);
                clickFlag = false;
                NavigationService.Navigate(new Uri("/Result.xaml", UriKind.RelativeOrAbsolute));
            }, delegate(Exception ex){
                ShowStatusBar(false);
                textBox1.IsReadOnly = false;
                textBox2.IsReadOnly = false;
                clickFlag = false;
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK);
            });
             
            
        }

        private void ShowStatusBar(string mes)
        {
            ProgressIndicator sysProg = new ProgressIndicator();
            sysProg.Text = mes;
            sysProg.IsVisible = true;
            SystemTray.SetProgressIndicator(this, sysProg);
        }

        private void ShowStatusBar(string mes, int value)
        {
            ProgressIndicator sysProg = new ProgressIndicator();
            sysProg.Text = mes;
            sysProg.Value = value;
            sysProg.IsVisible = true;
            SystemTray.SetProgressIndicator(this, sysProg);
        }

        private void ShowStatusBar(string mes, bool isIndeterminate)
        {
            ProgressIndicator sysProg = new ProgressIndicator();
            sysProg.Text = mes;
            sysProg.IsIndeterminate = isIndeterminate;
            sysProg.IsVisible = true;
            SystemTray.SetProgressIndicator(this, sysProg);
        }

        private void ShowStatusBar(bool isShow)
        {
            ProgressIndicator sysProg = new ProgressIndicator();
            sysProg.IsVisible = isShow;
            SystemTray.SetProgressIndicator(this, sysProg);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
            {
                IDictionary<string, string> param = NavigationContext.QueryString;
                // Textキーがあるかを確認して取得する
                if (param.ContainsKey("arr") && param.ContainsKey("dep"))
                {
                    textBox1.Text = (string)param["dep"];
                    textBox2.Text = (string)param["arr"];
                    textBox1.IsReadOnly = true;
                    textBox2.IsReadOnly = true;
                    button1_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    textBox1.IsReadOnly = false;
                    textBox2.IsReadOnly = false;
                }
            }
            else
            {
                textBox1.IsReadOnly = false;
                textBox2.IsReadOnly = false;
            }

            base.OnNavigatedTo(e);

        }
    }


}