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

        private void Button_Click(object sender, RoutedEventArgs e)
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
    }
}