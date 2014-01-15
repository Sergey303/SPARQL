using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using sema2012m;

namespace CommonRDF
{

    public class Graph : GraphBase
    {
        protected Dictionary<string, RecordEx> dics;
        protected Dictionary<string, string[]> n4;

        public override void CreateGraph()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetEntities()
        {
            return dics.Select(pair => pair.Key);
        }
        public override IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                var qu = rec.direct.SelectMany(axe =>
                {
                    string predicate = axe.predicate;
                    return axe.variants.Select(v => new PredicateEntityPair(predicate, v));
                });
                return qu;
            }
            return Enumerable.Empty<PredicateEntityPair>();
        }
        public override IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                var qu = rec.inverse.SelectMany(axe =>
                {
                    string predicate = axe.predicate;
                    return axe.variants.Select(v => new PredicateEntityPair(predicate, v));
                });
                return qu;
            }
            return Enumerable.Empty<PredicateEntityPair>();
        }
        public override IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                var qu = rec.data.SelectMany(axe =>
                {
                    string predicate = axe.predicate;
                    return axe.variants
                        .Select(data => data.Split('@'))
                        .Select(v => new PredicateDataTriple(predicate, v[0],
                            v.Length == 1 && v.Last().Length <= 4 ? v.Last() : null));
                });
                return qu;
            }
            return Enumerable.Empty<PredicateDataTriple>();
        }
        public override IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                Axe found = rec.direct.FirstOrDefault(ax => ax.predicate == predicate);
                if (found == null) return Enumerable.Empty<string>();
                return found.variants;
            }
            return Enumerable.Empty<string>();
        }
        public override IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                Axe found = rec.inverse.FirstOrDefault(ax => ax.predicate == predicate);
                if (found == null) return Enumerable.Empty<string>();
                return found.variants;
            }
            return Enumerable.Empty<string>();
        }
        public override IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                Axe found = rec.data.FirstOrDefault(ax => ax.predicate == predicate);
                if (found == null) return Enumerable.Empty<string>();
                return found.variants;
            }
            return Enumerable.Empty<string>();
        }
        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null)
        {
            RecordEx rec;
            if (dics.TryGetValue(id, out rec))
            {
                Axe found = rec.data.FirstOrDefault(ax => ax.predicate == predicate);
                if (found == null) return Enumerable.Empty<DataLangPair>();
                return found.variants.Select(d => 
                {
                    int ind = d.LastIndexOf("@");
                    string lang = null;
                    if (ind >= 0 && ind > d.Length - 6)
                    {
                        lang = d.Substring(ind + 1);
                        d = d.Substring(0, ind);
                    }
                    return new DataLangPair(d, lang);
                });
            }
            return Enumerable.Empty<DataLangPair>();
        }

        public override void GetItembyId(string id)
        {
            RecordEx re;
            if (dics.TryGetValue(id, out re))
            {
                Console.WriteLine("{0} {1}", id, re.rtype);
                foreach (var p in re.direct)
                {
                    Console.WriteLine("\t{0}", p.predicate);
                    foreach (var v in p.variants)
                    {
                        Console.WriteLine("\t\t{0}", v);
                    }
                }
            }
        }
        public override void Test()
        {
            //string id = "w20070417_5_8436";
            string id = "piu_200809051791";
            GetItembyId(id);
            string ss = "марч";
            SearchByName(ss);
        }

        public override void Load(params string[] rdfFiles)
        {
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

                dics = quads.GroupBy(q => q.entity)
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
                                    .Select(pv => new Axe() {predicate = pv.p, variants = pv.preds.ToArray()})
                                    .ToArray();
                            }
                            else if (va.vid == 2)
                            {
                                data = va.predvalues
                                    .Select(pv => new Axe() {predicate = pv.p, variants = pv.preds.ToArray()})
                                    .ToArray();
                            }
                            if (direct == null) direct = new Axe[0];
                            if (inverse == null) inverse = new Axe[0];
                            if (data == null) data = new Axe[0];
                        }
                        return new
                        {
                            id = q1.Key,
                            recExArr = new RecordEx() {rtype = q1.Key, direct = direct, inverse = inverse, data = data}
                        };
                    })
                    //.ToArray();
                    .ToDictionary(x => x.id, x => x.recExArr);
                // Теперь делаю словарь n4
                //Dictionary<string, string[]> 
                n4 = id_names
                    .Select(idna => new {na = new string(idna.Value.Take(4).ToArray()).ToLower(), id = idna.Key})
                    .GroupBy(naid => naid.na)
                    .ToDictionary(naids => naids.Key, naids => naids.Select(ni => ni.id).ToArray());
                // var nn = n4.Select(pair => pair.Value.Length).Max();

            }
        }

        public override string[] SearchByName(string ss)
        {
            string[] ids = null;
             if (!n4.TryGetValue(ss, out ids)) return ids;
             //Console.WriteLine("count=" + ids.Length);
             foreach (var id in ids)
             {
                 //var r = dics[id];
                 string[] names = dics[id].data.First(ax => ax.predicate == ONames.p_name).variants;
                 Console.Write(id);
                 foreach (var n in names)
                     Console.Write(" " + n);
                 Console.WriteLine();
             }
             return ids;
        }

        public override object GetNodeInfo(string id)
        {
            throw new NotImplementedException();
        }
    }
}
