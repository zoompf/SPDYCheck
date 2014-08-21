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

            SPDYResult result = SPDYChecker.Test("facebook.com", 443, 8000);

            int x = 45;

        }
    }
}
