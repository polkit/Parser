using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Net;

namespace Parser
{

    public class full
    {
        public full()
        {
            Fas = new List<Fa>();
        }
        public List<Fa> Fas { get; set; }
    }
    public class Fa
    {
        public string name { get; set; }

        public string reflink { get; set; }

        public MTable table { get; set; }

        public string parent { get; set; }
    }
    public class MTable
    {
        public MTable()
        {
            telements = new List<TableElem>();
        }
        public List<TableElem> telements { get; set; }
    }

    public class TableElem
    {
        public string pos { get; set; }
        public string group { get; set; }
        public string name { get; set; }
        public string price { get; set; }
    }

    class Program
    {

        static void Main(string[] args)
        {

            var url = $"/catalog/zapchasti_aksialno_porshnevykh_gidronasosov_i_gidromotorov/";
            Console.WriteLine("1 if Full Scan With CSV");
            Console.WriteLine("2 if Scan In MCSV");
            Console.WriteLine("3 if MCSV in CSV");
            var sw = Console.ReadLine();
            switch (sw)
            {
                case "1":
                    {
                        Console.WriteLine("Write FileName. default= res or res_sectime");
                        var nfilename = Console.ReadLine();
                        Console.WriteLine("Start");
                        Fill(url, null, 0);
                        WriteCSV(nfilename, f);
                        break;
                    }
                case "2":
                    {
                        Console.WriteLine("Write FileName. default= pres or pres_sectime");
                        var nfilename = Console.ReadLine();

                        if (string.IsNullOrEmpty(nfilename))
                            nfilename = "pres";
                        var path = Path.Combine(Directory.GetCurrentDirectory(), nfilename);
                        if (File.Exists($"{path}.mcsv"))
                            path += $"_{DateTimeOffset.UtcNow.UtcTicks}";
                        path += ".mcsv";
                        Console.WriteLine("Start");
                        Fill(url, null, 0);
                        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), path), Delln(Newtonsoft.Json.JsonConvert.SerializeObject(f)));
                        break;
                    }
                case "3":
                    {
                        Console.WriteLine("Write FileName in current directory. default= pres.mcsv");
                        var nfilename = Console.ReadLine();
                        if (string.IsNullOrEmpty(nfilename))
                            nfilename = "pres.mcsv";
                        var path = Path.Combine(Directory.GetCurrentDirectory(), nfilename);
                        if (!File.Exists(path))
                        {
                            Console.WriteLine($"file {path} doesnt exist");
                            break;
                        }
                        Console.WriteLine("Start");
                        var fs = Delln(File.ReadAllText(path));
                        var f = Newtonsoft.Json.JsonConvert.DeserializeObject<full>(fs);
                        WriteCSV(string.Empty, f);
                        break;
                    }
                default:
                    Console.WriteLine("Unknown Command");
                    break;
            }
            //Fill(url, null);
            //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "res.csv"), Newtonsoft.Json.JsonConvert.SerializeObject(f));
            //var fs = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "res.csv"));
            //var f = Newtonsoft.Json.JsonConvert.DeserializeObject<full>(fs);
            //WriteCSV(f);
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(f));

        }
        private static string Delln(string input)
        {
            var a = input.Replace("\\n", string.Empty);
            a = a.Replace(Environment.NewLine, "");
            return a;
        }
        private static void WriteCSV(string fileName, full f)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = "res";
            var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists($"{path}.csv"))
                path += $"_{DateTimeOffset.UtcNow.UtcTicks}";
            path += ".csv";
            List<string> text = new();
            text.Add($"modelname{CsvSeparator}parentlist{CsvSeparator}pos{CsvSeparator}group{CsvSeparator}name{CsvSeparator}price");
            foreach (var el in f.Fas)
            {
                if (el?.table?.telements != null)
                    foreach (var tabel in el.table.telements)
                        text.Add($"{el.name}{CsvSeparator}{el.parent}{CsvSeparator}{tabel.pos}{CsvSeparator}{tabel.group}{CsvSeparator}{tabel.name}{CsvSeparator}{tabel.price}");
                else
                    text.Add($"{el.name}{CsvSeparator}{el.parent}{CsvSeparator}{CsvSeparator}{CsvSeparator}{CsvSeparator}");
            }
            File.WriteAllLines(path, text, System.Text.Encoding.UTF8);
        }
        const string CsvSeparator = ";";

        private static full f = new full();
        private static string urlst = "https://www.siboma.ru";

        private static string GetResp(string furl)
        {
            WebRequest reqGET = WebRequest.Create(furl);
            WebResponse resp = reqGET.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            return sr.ReadToEnd();
        }
        private static void Fill(string url, string pparent, int count)
        {
            var furl = $"{urlst}{url}";


            string s = string.Empty;
            try
            {
                s = GetResp(furl);
            }
            catch
            {
                if (count < 5)
                    Fill(url, pparent, count++);
                else throw;
            }




            var parser = new HtmlParser();
            var document = parser.ParseDocument(s);
            var blueListItemsLinq = document.All.Where(m => m.LocalName == "tr" && m.ParentElement.LocalName == "tbody" && (m.ParentElement.ParentElement.ClassName == "main-catalog" || m.ParentElement.ParentElement.ParentElement.ClassName == "main-catalog")//&& !m.Children.Any(y => y.ClassName == "brand-img"
                                       );
            if (blueListItemsLinq.Any())
            {
                foreach (var elem in blueListItemsLinq)
                {
                    var trd = parser.ParseDocument(elem.InnerHtml).QuerySelector("a");
                    var ne = new Fa { reflink = trd.GetAttribute("href"), name = trd.TextContent, parent = pparent };
                    f.Fas.Add(ne);
                    Thread.Sleep(1000);
                    Console.WriteLine($"{ne.name}");
                    Fill($"{ne.reflink}", ne.name, 0);
                }
            }
            else
            {
                var tableElementList = document.All.Where(m => m.ClassName == "prod-row" && (m.ParentElement.ParentElement.ClassName == "det_table" || m.ParentElement.ParentElement.ParentElement.ClassName == "det_table")//&& !m.Children.Any(y => y.ClassName == "brand-img"
                                       );
                MTable ntable = new();
                foreach (var elem in tableElementList)
                {
                    var doc = parser.ParseDocument(elem.InnerHtml);
                    var trd = doc.All.Where(m => m.LocalName == "span" || m.LocalName == "a").ToList();
                    ntable.telements.Add(new TableElem { pos = trd[0].TextContent, group = trd[1].TextContent, name = trd[2].TextContent, price = trd[3].TextContent });
                }
                f.Fas.First(x => x.name == pparent).table = ntable;
            }
        }

    }
}
