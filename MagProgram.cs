using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CommonRDF
{
    class MagProgram : IReceiver
    {
        private GraphBase gr;
        private List<string[]> receive_list;
        public MagProgram(GraphBase gr)
        {
            this.gr = gr;
            Restart();
        }
        public void Restart() { receive_list = new List<string[]>();
        }
        public void Receive(string[] row) { receive_list.Add(row); }

        public void Run()
        {
            if (false) // проект freebase3m
            {
                foreach (var entity in gr.GetEntities().Take(10))
                {
                    Console.WriteLine("{0}", entity);
                }
                string idd = "ns:m.05ypwqv"; //"ns:m.0hz6pwx";
                //foreach (var qu in gr.GetData(idd)) Console.WriteLine("\t{0} {1}", qu.predicate, qu.data);
                XElement portrait = ((GraphTripletsTree) gr).GetPortraitSimple(idd, true);
                if (portrait != null) Console.WriteLine(portrait.ToString());

                return;
            }

            //string id2 = "piu_200809051791";
            //SimpleSparql ss = new SimpleSparql(id2);
            //Restart();
            //ss.Match(gr, this);
            //return;

            foreach (var person in new[]
            {
                "svet_100616111408_10844",
                "pavl_100531115859_2020",
                "piu_200809051791",
                "pavl_100531115859_6952",
                "svet_100616111408_10864",
                "w20090506_svetlana_5727",
                "piu_200809051742"
            })
            {
                SimpleSparql sims = new SimpleSparql(person);
                Restart();
               //  sims.Match(gr, this);
                //Perfomance.ComputeTime(() => sims.Match(gr, this), " mag test " + person + " ", true);
                Perfomance.ComputeTime(() => sims.Match(gr, this), " mag test bsbm 1 ", true);

                foreach (var row in receive_list)
                {
                    foreach (var e in row) Console.WriteLine("{0} ", e);
                    Console.WriteLine();
                }
            }





            // bool atleastonce = sims.Match(gr, this);
          //  Console.WriteLine("mag Sparql test ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
          //  if (!atleastonce) Console.WriteLine("false");
          //  else
            //{
            //    foreach (var row in receive_list)
            //    {
            //        foreach (string v in row) Console.Write(v + " ");
            //        Console.WriteLine();
            //    }
            //}
            //tt0 = DateTime.Now;
            //if (!atleastonce) Console.WriteLine("false");
            //Console.WriteLine("mag Sparql test 2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
