using System.IO;

namespace CommonRDF
{
    class LeshProgram
    {
        private readonly GraphBase gr;
        public LeshProgram(GraphBase gr)
        {
            this.gr = gr;
        }

        public void Run()
        {
            Query query = null;
            var text=File.ReadAllText(@"..\..\query.txt");
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
                string person1 = person;
                string textOfPerson = text.Replace("piu_200809051791", person1);
                Perfomance.ComputeTime(() =>
                    //@"..\..\query.txt"
                {
                    query = new Query(textOfPerson, gr);
                }, "read query for "+ person+" ", true);

                Perfomance.ComputeTime(query.Run, "run query for "+person+" ", true);

                if (query.SelectParameters.Count == 0)
                    query.OutputParamsAll(@"..\..\Output.txt");
                else
                    query.OutputParamsBySelect(@"..\..\Output.txt");
            }
            

        }
    }
}
