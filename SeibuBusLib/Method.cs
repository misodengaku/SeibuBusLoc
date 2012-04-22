using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using ParseHTML;
using Sgml;

namespace SeibuBusLib
{
    public class Location
    {

        /*static */public void SearchRoute(string _dep, string _arr, Action<List<BusInfo>> act, Action<Exception> exc)
        {
            List<BusInfo> bus = new List<BusInfo>();
            bool breakFlag = false;
            StringUtil.SjisEncoding sjisEncoding = new StringUtil.SjisEncoding();
            WebClient wc = new WebClient();
            string dep = StringUtil.UrlEncode(_dep, sjisEncoding);
            string arr = StringUtil.UrlEncode(_arr, sjisEncoding);

            //string addr = "http://locaaa.seibubus.co.jp/seibuloca/navi?VID=ssc&EID=nt&UKD=1&FSN=0&FSN=0&DSN=" + dep + "&ASN=" + arr;
            string addr = "http://loca.seibubus.co.jp/seibuloca/navi?VID=ssc&EID=nt&UKD=1&FSN=0&FSN=0&DSN=" + dep + "&ASN=" + arr;

            wc.Encoding = sjisEncoding;
            #region Delegate
            wc.OpenReadCompleted += delegate(object s, OpenReadCompletedEventArgs es)
            {

                if (es.Error != null)
                {
                    exc(es.Error);
                    return;
                }
                try
                {
                    using (var reader = new StreamReader(es.Result, sjisEncoding))
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
                                //.Skip(3)
                                .Elements(ns + "form")
                                .Elements(ns + "div")
                                //.Elements(ns + "div")
                                .Select(ex => new
                                {
                                    Text = ex.Element(ns + "div").Value,
                                });
                                errormes = error.First().Text.Replace("\t", "").Replace("\r\n", "");
                                exc(new Exception(errormes));
                            }
                            catch (Exception ec)
                            {
                                exc(ec);
                            }
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
                            var dep2 = int.Parse(tmp[0]);
                            tmp = array[2].Split(sep1, StringSplitOptions.RemoveEmptyEntries);
                            var arr2 = int.Parse(tmp[0]);
                            breakFlag = true;
                            wc.OpenReadAsync(new Uri("http://loca.seibubus.co.jp/seibuloca/navi?VID=lsc&EID=nt&UKD=1&FSN=0&FSN=0&DSMK=" + dep2 + "&ASMK=" + arr2));
                        }
                        else if(doc.ToString().Contains("乗車停留所または降車停留所の候補が複数あります。")){
                            MessageBox.Show("乗車停留所または降車停留所の候補が複数あります。");
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
                            breakFlag = false;
                            act(bus);
                            //MessageBox.Show(bus.First().Timetable);
                        }

                    }
                }
                catch (Exception ec)
                {
                    exc(ec);
                }
            };
            #endregion
            wc.OpenReadAsync(new Uri(addr, UriKind.RelativeOrAbsolute));
        }
    }
}
