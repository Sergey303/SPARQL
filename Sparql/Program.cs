using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparql
{
    class Program
    {
        public static void Main(string[] argp)
        {
            Scanner scnr = new Scanner();
            using (StreamReader file = new StreamReader("../../sparql data/queries/query10.rq"))
            {
                scnr.SetSource(file.ReadToEnd(), 0);
                scnr.yylex();
            }

        }
    }
}
