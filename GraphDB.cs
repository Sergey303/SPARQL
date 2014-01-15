using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PolarDB;
using sema2012m;

namespace CommonRDF
{
    class GraphDB : GraphBase
    {
        private PxCell pxGraph;
        private string path;
        public GraphDB(string path)
        {
            this.path = path;

            pxGraph = new PxCell(tp_graph, path + "\\data.pxc", false);
        }
        public override void Load(params string[] rdfFiles)
        {
            //  if (pxGraph.IsEmpty) return;
            foreach (var rdfFile in rdfFiles)
            {
                XElement db = XElement.Load(rdfFile);

                List<Quad> quads = new List<Quad>();
                List<KeyValuePair<string, string>> id_names = new List<KeyValuePair<string, string>>();
                var query = db.Elements() //.Take(1000)
                    .Where(el => el.Attribute(ONames.rdfabout) != null);
                foreach (XElement record in query)
                {
                    string about = record.Attribute(ONames.rdfabout).Value;
                    // Зафиксировать тип
                    quads.Add(new Quad(
                        0,
                        about,
                        ONames.rdftypestring,
                        record.Name.NamespaceName + record.Name.LocalName));
                    // Сканировать элементы
                    foreach (var prop in record.Elements())
                    {
                        // Есть разница между объектными свойствами и полями данных
                        string prop_name = prop.Name.NamespaceName + prop.Name.LocalName;
                        XAttribute rdfresource_att = prop.Attribute(ONames.rdfresource);
                        if (rdfresource_att != null)
                        {
                            quads.Add(new Quad(
                                0,
                                about,
                                prop_name,
                                rdfresource_att.Value));
                            quads.Add(new Quad(
                                1,
                                rdfresource_att.Value,
                                prop_name,
                                about));
                        }
                        else
                        {
                            string ex_data = prop.Value; // Надо продолжить!
                            XAttribute lang_att = prop.Attribute(ONames.xmllang);
                            if (lang_att != null) ex_data += "@" + lang_att.Value;
                            quads.Add(new Quad(
                                2,
                                about,
                                prop_name,
                                ex_data));
                            if (prop_name == "http://fogid.net/o/name")
                                id_names.Add(new KeyValuePair<string, string>(about, prop.Value));
                        }
                    }
                }
                // Буду строить вот такую структуру:

                pxGraph.Fill2(quads.GroupBy(q => q.entity)
                    .Select(q1 =>
                    {
                        string type_id = null;
                        Axe[] direct = null;
                        Axe[] inverse = new Axe[0];
                        Axe[] data = null;
                        var rea = q1.GroupBy(q => q.vid)
                            .Select(q2 => new
                            {
                                vid = q2.Key,
                                predvalues = q2.GroupBy(q => q.predicate)
                                    .Select(q3 => new {p = q3.Key, preds = q3.Select(q => q.rest).ToList()})
                                    .ToArray()
                            }).ToArray();
                        foreach (var va in rea)
                        {
                            if (va.vid == 0)
                            {
                                // Поиск первого типа (может не надо уничтожать запись???)
                                var qw = va.predvalues.FirstOrDefault(p => p.p == ONames.rdftypestring);
                                if (qw != null)
                                {
                                    type_id = qw.preds.First();
                                    //qw.preds.RemoveAt(0); // Уничтожение ссылки на тип
                                }
                                direct = va.predvalues
                                    .Select(pv => new Axe() {predicate = pv.p, variants = pv.preds.ToArray()})
                                    .ToArray();
                            }
                            else if (va.vid == 1)
                            {
                                inverse = va.predvalues
                                    .Select(pv => new Axe {predicate = pv.p, variants = pv.preds.ToArray()})
                                    .ToArray();
                            }
                            else if (va.vid == 2)
                            {
                                data = va.predvalues
                                    .Select(pv => new Axe {predicate = pv.p, variants = pv.preds.ToArray()})
                                    .ToArray();
                            }

                        }
                        if (direct == null) direct = new Axe[0];
                        if (inverse == null) inverse = new Axe[0];
                        if (data == null) data = new Axe[0];
                        return
                            new[]
                            {
                                (object) q1.Key, (object) q1.Key, direct.Select(Axe2Objects).ToArray(),
                                inverse.Select(Axe2Objects).ToArray(), data.Select(Axe2Objects).ToArray()
                            };
                    })
                    .OrderBy(x => x[0])
                    .ToArray());

            }
        }

        public override void CreateGraph()
        {
            throw new NotImplementedException();
        }

        private static object[]  Axe2Objects(Axe axe)
        {
            return new[]{(object)axe.predicate, axe.variants.Cast<object>().ToArray()};
        }


        public override void Test()
        {
          string id = "w20070417_5_8436";
            //string id = "piu_200809051791";
            var qu = pxGraph.Root.BinarySearchFirst(ent => - id.CompareTo(ent.Field(0).Get().Value));
            if (qu.IsEmpty) Console.WriteLine("no entry");
            else
            {
                var valu = qu.Get();
                Console.WriteLine(valu.Type.Interpret(valu.Value));
            }
        //    string ss = "марч";
         //   SearchByN4(ss);
        }

        public static readonly PType tp_axe= new PTypeRecord(
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("variants", new PTypeSequence(new PType(PTypeEnumeration.sstring))));
        public static readonly PType tp_graph= new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("rtype", new PType(PTypeEnumeration.sstring)),
                new NamedType("direct", new PTypeSequence(tp_axe)),
                new NamedType("inverse", new PTypeSequence(tp_axe)),
                new NamedType("data", new PTypeSequence(tp_axe))));


        public override IEnumerable<string> GetEntities()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null)
        {
            return GetProperties<PredicateEntityPair>(id, 2, predicate => value => new PredicateEntityPair(predicate, value));
        }

        public override IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null)
        {
            return GetProperties<PredicateEntityPair>(id, 3, predicate=> value => new PredicateEntityPair(predicate, value));
        }

       // public static Regex LangRegex=new Regex("");
        public override IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null)
        {
            return GetProperties<PredicateDataTriple>(id, 4, predicate => value =>
            {
                var dataLang = SplitLang(value);
                return new PredicateDataTriple(predicate, dataLang.data,dataLang.lang);
            });
        }

        private IEnumerable<T> GetProperties<T>(string id, int direction, Func<string,Func<string,T>> selector)
        {
            var qu = pxGraph.Root.BinarySearchFirst(ent => -id.CompareTo(ent.Field(0).Get().Value));
            if (qu.IsEmpty) return Enumerable.Empty<T>();
            return ((object[])((object[])qu.Get().Value)[direction])
                .Cast<object[]>()
                .SelectMany(axe => ((object[]) axe[1]).Cast<string>().Select(selector((string) axe[0])));
        }
        public override IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null)
        {
            return GetProperties(id, predicate, 2);
        }

        public override IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null)
        {
            return GetProperties(id, predicate, 3);
        }

        private IEnumerable<string> GetProperties(string id, string predicate, int direction)
        {
            var qu = pxGraph.Root.BinarySearchFirst(ent => -id.CompareTo(ent.Field(0).Get().Value));
            if (qu.IsEmpty) return Enumerable.Empty<string>();
            var axe = ((object[]) ((object[]) qu.Get().Value)[direction])
                .Cast<object[]>()
                .FirstOrDefault(axe1 => (string) axe1[0] == predicate);
            return axe == null ? Enumerable.Empty<string>() : ((object[])axe[1]).Cast<string>();
        }

        public override IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null)
        {
            return GetProperties(id, predicate, 4);
        }

        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null)
        {
            return GetData(id, predicate)
                .Select(SplitLang);
        }
        public override void GetItembyId(string id)
        {
            var qu = pxGraph.Root.BinarySearchFirst(ent => -id.CompareTo(ent.Field(0).Get().Value));
            if (qu.IsEmpty) Console.WriteLine("no entry");
            else
            {
                var valu = qu.Get();
                Console.WriteLine(valu.Type.Interpret(valu.Value));
            }
        }

      

        public override string[] SearchByName(string ss)
        {
            throw new NotImplementedException();
        }

        public override object GetNodeInfo(string id)
        {
            throw new NotImplementedException();
        }
    }
}