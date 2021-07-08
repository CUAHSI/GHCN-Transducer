using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDEC_Harvester
{
    class sandbox
    {
        struct stationSeriesData
        {
            public string SensorDescription;
            public string SensorNumber;
            public string Plot;
            public string Data;
            public string Collection;
            public string DataAvailable;
        }

        public void test()
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument doc = web.Load("https://cdec.water.ca.gov/dynamicapp/staMeta?station_id=yub");

            //DataTable table = new DataTable();
            //var headers = doc.DocumentNode.SelectNodes("//table[2]//tbody//tr");
            //foreach (HtmlNode header in headers)
            //    table.Columns.Add(header.InnerText); // create columns from th
            //                                         // select rows with td elements 
            var seriesList = new List<stationSeriesData>();

            //TestSelect(doc, "//table[2]//tr//td");
            //some sites have additional table e.g yub so pick second to last as all seem to have the additional commnets table which can be empty 
            var tablecount = doc.DocumentNode.SelectNodes("//table").Count;
            var tableindex = tablecount - 1;

            foreach (var row in doc.DocumentNode.SelectNodes("//table["+ tableindex + "]//tr"))
            {
                var stat = new stationSeriesData();
                var tds = row.Descendants("td");
                for (int i =0; i< tds.Count(); i++)
                {
                    if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i==0)
                    {
                        stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                        Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml));
                    }
                    if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 1)
                    {
                        stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                        Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml));
                    }
                    if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 2)
                    {
                        stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart());

                        Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart()));
                    }
                    if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 3)
                    {
                        stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart());

                        Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart()));
                    }
                    if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 4)
                    {
                        stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                        Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml));
                    }
                    if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 5)
                    {
                        stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                        Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.TrimStart()));
                    }                    
                }
                seriesList.Add(stat);
            }

        }

        static void TestSelect(HtmlDocument htmlDoc, string xpath)
        {
            Console.WriteLine("\nInput path: " + xpath);
            var splitPath = xpath.Split('/');
            for (int i = 2; i <= splitPath.Length; i++)
            {
                if (splitPath[i - 1] == "")
                    continue;
                var thisPath = string.Join("/", splitPath, 0, i);
                Console.Write("Testing \"{0}\": ", thisPath);
                var result = htmlDoc.DocumentNode.SelectNodes(thisPath);
                Console.WriteLine("result count = {0}", result == null ? "null" : result.Count.ToString());
            }
        }
        internal static string RemoveUnwantedTags(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            var document = new HtmlDocument();
            document.LoadHtml(data);

            var acceptableTags = new String[] { "strong", "em", "u" };

            var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                {
                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);

                }
            }

            return document.DocumentNode.InnerHtml;
        }
    }
}