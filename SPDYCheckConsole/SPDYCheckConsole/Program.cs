using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zoompf.SPDYAnalysis;

namespace Zoompf.SPDYCheckConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            SPDYResult result = SPDYChecker.Test("www.qlv.berlin", 443, 8000, "10.10.10.10");

            int x = 45;

        }
    }
}
