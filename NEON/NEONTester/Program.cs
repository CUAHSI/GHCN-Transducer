using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONTester
{
    class Program
    {
        static void Main(string[] args)
        {
            //var tester = new WebServiceTester("http://hydroportal.cuahsi.org/NEON/cuahsi_1_1.asmx");
            var tester = new WebServiceTester("http://localhost:6278/cuahsi_1_1.asmx");
            tester.Run();
        }
    }
}
