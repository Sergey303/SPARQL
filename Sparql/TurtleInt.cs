using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public class TurtleInt
    {
        public static IEnumerable<TripleInt> LoadGraph(string datafile)//EngineVirtuoso engine, string graph, string datafile)
        {
            int ntriples = 0;
            string subject = null;
            Dictionary<string, string> namespaces = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(datafile);
            int count = 200000000;
            for (int i = 0; i < count; i++)
            {
                string line = sr.ReadLine();
                //if (i % 10000 == 0) { Console.Write("{0} ", i / 10000); }
                if (line == null) break;
                if (line == "") continue;
                if (line[0] == '@')
                { // namespace
                    string[] parts = line.Split(' ');
                    if (parts.Length != 4 || parts[0] != "@prefix" || parts[3] != ".")
                    {
                        Console.WriteLine("Err: strange line: " + line);
                        continue;
                    }
                    string pref = parts[1];
                    string nsname = parts[2];
                    if (nsname.Length < 3 || nsname[0] != '<' || nsname[nsname.Length - 1] != '>')
                    {
                        Console.WriteLine("Err: strange nsname: " + nsname);
                        continue;
                    }
                    nsname = nsname.Substring(1, nsname.Length - 2);
                    namespaces.Add(pref, nsname);
                }
                else if (line[0] != ' ')
                { // Subject
                    line = line.Trim();
                    subject = GetEntityString(namespaces, line);
                    if (subject == null) continue;
                }
                else
                { // Predicate and object
                    string line1 = line.Trim();
                    int first_blank = line1.IndexOf(' ');
                    if (first_blank == -1) { Console.WriteLine("Err in line: " + line); continue; }
                    string pred_line = line1.Substring(0, first_blank);
                    string predicate = GetEntityString(namespaces, pred_line);
                    string rest_line = line1.Substring(first_blank + 1).Trim();
                    // Уберем последний символ
                    rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                    bool isDatatype = rest_line[0] == '\"';
                    // объект может быть entity или данное, у данного может быть языковый спецификатор или тип
                    string entity = null;
                    string sdata = null;
                    string datatype = null;
                    string lang = null;
                    if (isDatatype)
                    {
                        // Последняя двойная кавычка 
                        int lastqu = rest_line.LastIndexOf('\"');

                        // Значение данных
                        sdata = rest_line.Substring(1, lastqu - 1);

                        // Языковый специализатор:
                        int dog = rest_line.LastIndexOf('@');
                        if (dog == lastqu + 1) lang = rest_line.Substring(dog + 1, rest_line.Length - dog - 1);
 
                        int pp = rest_line.IndexOf("^^");
                        if (pp == lastqu + 1)
                        {
                            //  Тип данных
                            string qname = rest_line.Substring(pp + 2);
                            //  тип данных может быть "префиксным" или полным
                            if (qname[0] == '<')
                            {
                                datatype = qname.Substring(1, qname.Length - 2);
                            }
                            else
                            {
                                datatype = GetEntityString(namespaces, qname);
                            }
                        }
                        yield return new DTripleInt()
                        {
                            subject = TripleInt.Code(subject),
                            predicate = TripleInt.Code(predicate),
                            data = // d
                                datatype == "http://www.w3.org/2001/XMLSchema#integer" ?
                                    new Literal() { vid = LiteralVidEnumeration.integer, value = int.Parse(sdata) } :
                                (datatype == "http://www.w3.org/2001/XMLSchema#date" ?
                                    new Literal() { vid = LiteralVidEnumeration.date, value = DateTime.Parse(sdata).ToBinary() } :
                                (new Literal() { vid = LiteralVidEnumeration.text, value = new Text() { s = sdata, l = "en" } }))

                        };
                    }
                    else
                    { // entity
                        entity = rest_line[0] == '<' ? rest_line.Substring(1, rest_line.Length-2) : GetEntityString(namespaces, rest_line);
                        yield return new OTripleInt() 
                        {
                            subject = TripleInt.Code(subject),
                            predicate = TripleInt.Code(predicate),
                            obj = TripleInt.Code(entity) 
                        };
                    }
                    ntriples++;
                }
            }
            Console.WriteLine("ntriples={0}", ntriples);
        }

        private static string GetEntityString(Dictionary<string, string> namespaces, string line)
        {
            string subject = null; 
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!namespaces.ContainsKey(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = namespaces[prefix] + line.Substring(colon + 1);
            End: 
            return subject;
        }
    }
}
