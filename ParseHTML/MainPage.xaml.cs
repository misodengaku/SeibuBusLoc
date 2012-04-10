using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Sgml;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Phone.Shell;

namespace ParseHTML
{
    public partial class MainPage : PhoneApplicationPage
    {
        StringUtil.SjisEncoding sjisEncoding = new StringUtil.SjisEncoding();
        WebClient wc = new WebClient();
        List<BusInfo> bus = new List<BusInfo>();
        bool breakFlag = false, clickFlag = false;

        // コンストラクター
        public MainPage()
        {
            InitializeComponent();
            wc.Encoding = sjisEncoding;
            wc.OpenReadCompleted += wc_OpenReadCompleted;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (clickFlag)
                return;
            if (!Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("ネットワークに接続していません");
                return;
            }
            //MessageBox.Show(""+StringUtil.UrlEncode("朝霞駅", sjisEncoding));
            if (textBox1.Text.Length == 0 || textBox2.Text.Length == 0)
            {
                MessageBox.Show("停留所名を入力してください", "エラー", MessageBoxButton.OK);
                return;
            }

            clickFlag = true;

            string dep = StringUtil.UrlEncode(textBox1.Text, sjisEncoding);
            string arr = StringUtil.UrlEncode(textBox2.Text, sjisEncoding);

            wc.OpenReadAsync(new Uri("http://loca.seibubus.co.jp/seibuloca/navi?VID=ssc&EID=nt&UKD=1&FSN=0&FSN=0&DSN=" + dep + "&ASN=" + arr));
            
        }


        void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                return;
            }
            ShowStatusBar("検索中...", 25);
            try
            {
                using (var reader = new StreamReader(e.Result, sjisEncoding))
                using (var sgmlReader = new SgmlReader { InputStream = reader })
                {
                    sgmlReader.DocType = "HTML";
                    sgmlReader.CaseFolding = CaseFolding.ToLower;

                    // HTMLを元にXDocumentを作成。
                    var doc = XDocument.Load(sgmlReader);
                    var ns = doc.Root.Name.Namespace;
                    if (doc.ToString().Contains("error-box"))
                    {
                        string errormes = "";
                        try
                        {
                            var error = doc.Descendants(ns + "body")
                            .Elements(ns + "div")
                            .Elements(ns + "div")
                            .Skip(3)
                            .Select(ex => new
                            {
                                Text = ex.Element(ns + "div").Value,
                            });
                            errormes = error.First().Text.Replace("\t", "").Replace("\r\n", "");
                        }
                        catch { }
                        MessageBox.Show(errormes, "エラー", MessageBoxButton.OK);
                        ShowStatusBar(false);
                    }
                    else if (doc.ToString().Contains("document.SubmitForm.DSMK.value") && breakFlag != true)
                    {
                        var error = doc.Descendants(ns + "head")

                            .Elements(ns + "script")
                            .Skip(2)
                            .Select(ex => new
                            {
                                Text = ex.ToString()
                            });

                        string[] sep = { "document.SubmitForm.DSMK.value = \"", "document.SubmitForm.ASMK.value = \"" };
                        string[] array = error.First().Text.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        string[] sep1 = { "\"" };

                        var tmp = array[1].Split(sep1, StringSplitOptions.RemoveEmptyEntries);
                        var dep = int.Parse(tmp[0]);
                        tmp = array[2].Split(sep1, StringSplitOptions.RemoveEmptyEntries);
                        var arr = int.Parse(tmp[0]);
                        ShowStatusBar("検索中...", 50);
                        breakFlag = true;
                        wc.OpenReadAsync(new Uri("http://loca.seibubus.co.jp/seibuloca/navi?VID=lsc&EID=nt&UKD=1&FSN=0&FSN=0&DSMK=" + dep + "&ASMK=" + arr));
                    }
                    else
                    {
                        //MessageBox.Show(doc.ToString());
                        var content = doc.Descendants(ns + "body")
                            .Elements(ns + "div")
                            .Elements(ns + "div")
                            .Skip(3)
                            .Descendants(ns + "tr")
                            .Skip(1)
                            .Select(ex => new// BusInfo
                            {
                                Text = ex.ToString()
                            });
                        ShowStatusBar("検索中...", 75);
                        bus.Clear();
                        foreach (var item in content)
                        {
                            string[] sep = { "</td>" };
                            char[] sep1 = { '>' };
                            string[] data = item.Text.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            BusInfo nb = new BusInfo();
                            nb.Timetable = data[0].Split(sep1)[2];
                            nb.DepEst = data[1].Split(sep1)[1];
                            nb.EstMes = data[2].Split(sep1)[1];
                            nb.ArrEst = data[3].Split(sep1)[1];
                            nb.Platform = data[4].Split(sep1)[1];
                            nb.Route = data[5].Split(sep1)[1];
                            nb.Dest = data[6].Split(sep1)[1];
                            nb.Type = data[7].Split(sep1)[1];
                            bus.Add(nb);
                            //MessageBox.Show(data[0]);
                        }
                        PhoneApplicationService.Current.State["Route"] = textBox1.Text + "→" + textBox2.Text;
                        PhoneApplicationService.Current.State["Info"] = bus;
                        ShowStatusBar(false);
                        clickFlag = false;
                        breakFlag = false;
                        NavigationService.Navigate(new Uri("/Result.xaml", UriKind.RelativeOrAbsolute));
                        //MessageBox.Show(bus.First().Timetable);
                    }

                }
            }
            catch (Exception ec)
            {
                MessageBox.Show("Exception " + ec.Message);
            }

            clickFlag = false;
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
    }

    public class BusInfo
    {
        //public string Time{get;set;}
        //public string Route{get;set;}
        public string Timetable { get; set; }
        public string DepEst { get; set; }
        public string EstMes { get; set; }
        public string ArrEst { get; set; }
        public string Platform { get; set; }
        public string Route { get; set; }
        public string Dest { get; set; }
        public string Type { get; set; }
    }

}