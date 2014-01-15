using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using sema2012m;

namespace CommonRDF
{
    public class GraphDBmy : Graph
    {
        private static PType tp_axe;
        private static PType tp_graph;
        private void InitTypes()
        {
            tp_axe = new PTypeRecord(
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("variants", new PTypeSequence(new PType(PTypeEnumeration.sstring))));
            tp_graph = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("rtype", new PType(PTypeEnumeration.sstring)),
                new NamedType("direct", new PTypeSequence(tp_axe)),
                new NamedType("inverse", new PTypeSequence(tp_axe)),
                new NamedType("data", new PTypeSequence(tp_axe))));
        }
        private string path;
        PxCell cell;
        public GraphDBmy(string path) 
        {
            this.path = path;
            InitTypes();
            cell = new PxCell(tp_graph, path + "graph.pxc", false);
        }
        public override void Test()
        {
            string id = "w20070417_5_8436";
            //string id = "piu_200809051791";
            var qu = cell.Root.BinarySearchFirst(ent => - id.CompareTo(ent.Field(0).Get().Value));
            if (qu.IsEmpty) Console.WriteLine("no entry");
            else
            {
                var valu = qu.Get();
                Console.WriteLine(valu.Type.Interpret(valu.Value));
            }
        }
        private struct RecordEx2
        {
            public string id;
            public string rtype;
            public Axe[] direct;
            public Axe[] inverse;
            public Axe[] data;
        }
        public void Load()
        {

            XElement db = XElement.Load(path + "0001.xml");

            List<Quad> quads = new List<Quad>();
            //List<KeyValuePair<string, string>> id_names = new List<KeyValuePair<string, string>>();
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
                        //if (prop_name == "http://fogid.net/o/name")
                        //    id_names.Add(new KeyValuePair<string, string>(about, prop.Value));
                    }
                }
            }
            // Буду строить вот такую структуру:
            RecordEx2[] dics;
            dics = quads.GroupBy(q => q.entity)
                .Select(q1 =>
                {
                    string id = q1.Key;
                    string type_id = null;
                    Axe[] direct = null;
                    Axe[] inverse = new Axe[0];
                    Axe[] data = null;
                    var rea = q1.GroupBy(q => q.vid)
                        .Select(q2 => new
                        {
                            vid = q2.Key,
                            predvalues = q2.GroupBy(q => q.predicate)
                                .Select(q3 => new { p = q3.Key, preds = q3.Select(q => q.rest).ToList() })
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
                                .Select(pv => new Axe() { predicate = pv.p, variants = pv.preds.ToArray() })
                                .ToArray();
                        }
                        else if (va.vid == 1)
                        {
                            inverse = va.predvalues
                                .Select(pv => new Axe() { predicate = pv.p, variants = pv.preds.ToArray() })
                                .ToArray();
                        }
                        else if (va.vid == 2)
                        {
                            data = va.predvalues
                                .Select(pv => new Axe() { predicate = pv.p, variants = pv.preds.ToArray() })
                                .ToArray();
                        }
                        if (direct == null) direct = new Axe[0];
                        if (inverse == null) inverse = new Axe[0];
                        if (data == null) data = new Axe[0];
                    }
                    return new RecordEx2() { id = q1.Key, rtype = q1.Key, direct = direct, inverse = inverse, data = data };
                    //return new 
                    //{
                    //    id = q1.Key,
                    //    recExArr = new RecordEx2() { rtype = q1.Key, direct = direct, inverse = inverse, data = data }
                    //};
                })
                .OrderBy(x => x.id)
                .ToArray();
                //.ToDictionary(x => x.id, x => x.recExArr);

            var pobj = dics.Select(rex => 
                new object[] {
                    rex.id, 
                    rex.rtype,
                    rex.direct.Select(a => new object[] {a.predicate, a.variants}).ToArray(),
                    rex.inverse.Select(a => new object[] {a.predicate, a.variants}).ToArray(),
                    rex.data.Select(a => new object[] {a.predicate, a.variants}).ToArray()
                }).ToArray();
            cell.Fill2(pobj);
        }
    }
}
