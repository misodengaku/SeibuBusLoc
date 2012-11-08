using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SeibuBusLib;

namespace SeibuBus
{
    public partial class Result : PhoneApplicationPage
    {
        bool clickFlag = false;
        public Result()
        {
            InitializeComponent();
            try
            {
                var query = (List<BusInfo>)PhoneApplicationService.Current.State["Info"];
                timeInfoList.ItemsSource = query;
                PageTitle.Text = (string)PhoneApplicationService.Current.State["Route"];
            }
            catch { }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            try
            {
                var data = new Microsoft.Phone.Shell.StandardTileData()
                {
                    BackgroundImage = new Uri("/bustile_big.png", UriKind.Relative),
                    Title = PageTitle.Text//"Twitterに投稿"
                };
                var arr = (string)PhoneApplicationService.Current.State["Arr"];
                var dep = (string)PhoneApplicationService.Current.State["Dep"];

                // セカンダリタイルがタップされた時に遷移するUri(同一アプリ内からのみ可能)
                var naviUrl = new Uri("/MainPage.xaml?dep=" + dep + "&arr=" + arr, UriKind.Relative);

                // タイルを追加する
                Microsoft.Phone.Shell.ShellTile.Create(naviUrl, data);
            }
            catch
            {
                MessageBox.Show("すでにタイルが存在します", "エラー", MessageBoxButton.OK);
            }
        }
        private void backButton_Click(object sender, EventArgs e)
        {
            if (clickFlag)
                return;
            string from = (string)PhoneApplicationService.Current.State["Dep"];
            string to = (string)PhoneApplicationService.Current.State["Arr"];

            ShowStatusBar("逆方向の情報を取得中...", true);
            Location.SearchRoute(to, from, bus =>
            {
                PhoneApplicationService.Current.State["Route"] = to + "→" + from;
                PhoneApplicationService.Current.State["Arr"] = from;
                PhoneApplicationService.Current.State["Dep"] = to;
                PhoneApplicationService.Current.State["Info"] = bus;
                clickFlag = false;
                Dispatcher.BeginInvoke(() =>
                {
                    ShowStatusBar(false);
                    try
                    {
                        var query = (List<BusInfo>)PhoneApplicationService.Current.State["Info"];
                        timeInfoList.ItemsSource = query;
                        PageTitle.Text = (string)PhoneApplicationService.Current.State["Route"];
                    }
                    catch { }
                });
            }, ex =>
            {
                clickFlag = false;
                Dispatcher.BeginInvoke(() =>
                {
                    ShowStatusBar(false);
                    MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK);
                });
            });
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            if (clickFlag)
                return;
            string from = (string)PhoneApplicationService.Current.State["Dep"];
            string to = (string)PhoneApplicationService.Current.State["Arr"];

            ShowStatusBar("最新の情報に更新中...", true);
            Location.SearchRoute(from, to, bus =>
            {
                PhoneApplicationService.Current.State["Route"] = from + "→" + to;
                PhoneApplicationService.Current.State["Arr"] = to;
                PhoneApplicationService.Current.State["Dep"] = from;
                PhoneApplicationService.Current.State["Info"] = bus;
                clickFlag = false;
                Dispatcher.BeginInvoke(() =>
                {
                    ShowStatusBar(false);
                    try
                    {
                        var query = (List<BusInfo>)PhoneApplicationService.Current.State["Info"];
                        timeInfoList.ItemsSource = query;
                        PageTitle.Text = (string)PhoneApplicationService.Current.State["Route"];
                    }
                    catch { }
                });
            }, ex =>
            {
                clickFlag = false;
                Dispatcher.BeginInvoke(() =>
                {
                    ShowStatusBar(false);
                    MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK);
                });
            });
        }
        #region StatusBar
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
        #endregion
    }
}