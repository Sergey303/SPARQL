using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using sema2012m;

namespace TrueRdfViewer
{
    public class TripleStoreInt
    {
        private PType tp_otriple_seq;
        private PType tp_dtriple_seq;
        private PType tp_entity;
        private PType tp_dtriple_spf; 
        private void InitTypes()
        {
            tp_entity = new PType(PTypeEnumeration.integer);
            PType tp_rliteral = new PTypeUnion(
                new NamedType("void", new PType(PTypeEnumeration.none)),
                new NamedType("integer", new PType(PTypeEnumeration.integer)),
                new NamedType("string", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)));
            tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("object", tp_entity)));
            tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("data", tp_rliteral)));
            // Тип для экономного выстраивания индекса s-p для dtriples
            tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("offset", new PType(PTypeEnumeration.longinteger))));

        }
        private string path;
        private PaCell otriples;
        private PaCell otriples_op; // объектные триплеты, упорядоченные по o-p
        private PaCell dtriples;
        private PaCell dtriples_sp;
        private FlexIndexView<SubjPredObjInt> spo_o_index = null;
        private FlexIndexView<SubjPredInt> sp_d_index = null;
        private FlexIndexView<SubjPredInt> op_o_index = null;
        private PaCell oscale;
        private int range = 0;

        public TripleStoreInt(string path)
        {
            this.path = path;
            InitTypes();
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            otriples_op = new PaCell(tp_otriple_seq, path + "otriples_op.pac", false);
            dtriples = new PaCell(tp_dtriple_seq, path + "dtriples.pac", false);
            dtriples_sp = new PaCell(tp_dtriple_spf, path + "dtriples_spf.pac", false);
            oscale = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "oscale.pac", false);
            if (!oscale.IsEmpty)
            {
                CalculateRange();
            }
            if (!otriples.IsEmpty)
            {
                OpenCreateIndexes();
            }
        }

        private void CalculateRange()
        {
            long len = oscale.Root.Count() - 1;
            int r = 1;
            while (len != 0) { len = len >> 1; r++; }
            range = r + 4;
        }

        private void OpenCreateIndexes()
        {
            spo_o_index = new FlexIndexView<SubjPredObjInt>(path + "spo_o_index", otriples.Root,
                ent => new SubjPredObjInt() { subj = (int)ent.Field(0).Get(), pred = (int)ent.Field(1).Get(), obj = (int)ent.Field(2).Get() });
            sp_d_index = new FlexIndexView<SubjPredInt>(path + "subject_d_index", dtriples.Root,
                ent => new SubjPredInt() { subj = (int)ent.Field(0).Get(), pred = (int)ent.Field(1).Get() });
            op_o_index = new FlexIndexView<SubjPredInt>(path + "obj_o_index", otriples.Root,
                ent => new SubjPredInt() { subj = (int)ent.Field(2).Get(), pred = (int)ent.Field(1).Get() });
            CreateScale();
            long ntriples = otriples.Root.Count();
            ShowScale(ntriples);
        }

        public void LoadTurtle(string filepath)
        {
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            foreach (var triple in TurtleInt.LoadGraph(filepath))
            {
                if (i % 10000 == 0) Console.Write("{0} ", i / 10000);
                i++;
                if (triple is OTripleInt)
                {
                    var tr = (OTripleInt)triple;
                    //Console.WriteLine(tr.obj);
                    //otriples.Root.AppendElement(new object[] { tr.subject, tr.predicate, tr.obj });
                    otriples.Root.AppendElement(new object[] 
                    { 
                        tr.subject, 
                        tr.predicate, 
                        tr.obj 
                    });
                }
                else
                {
                    var tr = (DTripleInt)triple;
                    Literal lit = tr.data;
                    object[] da;
                    if (lit.vid == LiteralVidEnumeration.integer)
                        da = new object[] { 1, lit.value };
                    else if (lit.vid == LiteralVidEnumeration.date)
                        da = new object[] { 3, lit.value };
                    else if (lit.vid == LiteralVidEnumeration.text)
                    {
                        Text t = (Text)lit.value;
                        da = new object[] { 2, new object[] { t.s, t.l } };
                    }
                    else
                        da = new object[] { 0, null };
                    //dtriples.Root.AppendElement(new object[] { tr.subject, tr.predicate, da });
                    dtriples.Root.AppendElement(new object[] 
                    { 
                        tr.subject, 
                        tr.predicate, 
                        da 
                    });
                }
            }
            Console.WriteLine();
            otriples.Flush();
            dtriples.Flush();
            
            SPOComparer spo_compare = new SPOComparer();
            SPComparer sp_compare = new SPComparer();
            // Создание и упорядочивание дополнительных структур
            otriples_op.Clear();
            otriples_op.Fill(new object[0]);
            foreach (object v in otriples.Root.ElementValues()) otriples_op.Root.AppendElement(v);
            otriples_op.Flush();
            dtriples_sp.Clear();
            dtriples_sp.Fill(new object[0]);
            foreach (PaEntry entry in dtriples.Root.Elements())
            {
                int s = (int)entry.Field(0).Get();
                int p = (int)entry.Field(1).Get();
                dtriples_sp.Root.AppendElement(new object[] { s, p, entry.offset });
            }
            dtriples_sp.Flush();
            // Упорядочивание otriples по s-p-o
            otriples.Root.SortByKey<SubjPredObjInt>(rec => new SubjPredObjInt(rec), spo_compare);
            // Упорядочивание otriples_op по o-p
            otriples_op.Root.SortByKey<SubjPredInt>(rec => 
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[2] };
            }, sp_compare);
            // Упорядочивание dtriples_sp по s-p
            dtriples_sp.Root.SortByKey<SubjPredInt>(rec =>
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[0] };
            }, sp_compare);

            // Индексирование
            if (spo_o_index == null)
            {
                OpenCreateIndexes();
            }
            spo_o_index.Load(spo_compare);

            sp_d_index.Load(sp_compare);
            op_o_index.Load(sp_compare);
            // Создание шкалы (Надо переделать)
            //CreateScale();
            //ShowScale();
            //oscale.Clear();
            //oscale.Fill(new object[0]);
            //foreach (int v in scale.Values()) oscale.Root.AppendElement(v);
            //oscale.Flush();
            //CalculateRange(); // Наверное, range считается в CreateScale() 
        }
        //public void LoadXML(string filepath)
        //{
        //    otriples.Clear();
        //    otriples.Fill(new object[0]);
        //    dtriples.Clear();
        //    dtriples.Fill(new object[0]);
        //    XElement db = XElement.Load(filepath);
        //    foreach (XElement elem in db.Elements())
        //    {
        //        var id_att = elem.Attribute(ONames.rdfabout);
        //        if (id_att == null) continue;
        //        string id = id_att.Value;
        //        string typ = elem.Name.NamespaceName + elem.Name.LocalName;
        //        otriples.Root.AppendElement(
        //            new object[] { id, ONames.rdfnsstring + "type", typ } );
        //        foreach (XElement el in elem.Elements())
        //        {
        //            string prop = el.Name.NamespaceName + el.Name.LocalName;
        //            var resource_att = el.Attribute(ONames.rdfresource);
        //            if (resource_att != null)
        //            { // Объектная ссылка
        //                otriples.Root.AppendElement(
        //                    new object[] { id, prop, resource_att.Value } );
        //            }
        //            else
        //            { // Поле данных
        //                var lang_att = el.Attribute(ONames.xmllang);
        //                string lang = lang_att == null ? "" : lang_att.Value;
        //                dtriples.Root.AppendElement(
        //                    new object[] { id, prop, 
        //                        new object[] { 2, new object[] { el.Value, lang } } } );
        //            }
        //        }
        //    }
        //    otriples.Flush();
        //    dtriples.Flush();
        //    // Индексирование
        //    if (!otriples.IsEmpty)
        //    {
        //        OpenCreateIndexes();
        //    }
        //    spo_o_index.Load(null);
        //    sp_d_index.Load(null);
        //    op_o_index.Load(null);
        //    // Создание шкалы
        //    CreateScale();
        //    ShowScale();
        //    oscale.Clear();
        //    oscale.Fill(new object[0]);
        //    foreach (int v in scale.Values()) oscale.Root.AppendElement(v);
        //    oscale.Flush();
        //}

        private Scale1 scale = null;
        private void CreateScale()
        {
            long len = otriples.Root.Count() - 1;
            int r = 1;
            while (len != 0) { len = len >> 1; r++; }

            range = r + 4; // здесь 4 - фактор "разрежения" шкалы, можно меньше
            scale = new Scale1(range);
            foreach (object[] tr in otriples.Root.ElementValues())
            {
                int subj = (int)tr[0];
                int pred = (int)tr[1];
                int obj = (int)tr[2];
                int code = Scale1.Code(range, subj, pred, obj);
                scale[code] = 1;
            }
        }
        public void ShowScale(long ntriples)
        {
            int c = scale.Count();
            int c1 = 0;
            for (int i=0; i<c; i++)
            {
                int bit = scale[i];
                if (bit > 0) c1++;
            }
            Console.WriteLine("{0} {1} {2}", c, c1, ntriples);
        }

        public IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            var query = otriples_op.Root.BinarySearchAll(ent =>
                {
                    object[] rec = (object[])ent.Get();
                    int ob = (int)rec[2];
                    int cmp = ob.CompareTo(obj);
                    if (cmp != 0) return cmp;
                    int pr = (int)rec[1];
                    return pr.CompareTo(pred);
                });
            return query
                .Select(en => (int)en.Field(0).Get());
        }
        public IEnumerable<int> GetSubjectByObjPred0(int obj, int pred)
        {
            return op_o_index.GetAll(ent => 
            {
                int ob = (int)ent.Field(2).Get();
                int cmp = ob.CompareTo(obj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int)en.Field(0).Get());
        }
        public IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            return otriples.Root.BinarySearchAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int)en.Field(2).Get());
        }
        public IEnumerable<int> GetObjBySubjPred0(int subj, int pred)
        {
            return spo_o_index.GetAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int)en.Field(2).Get());
        }
        public IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (dtriples.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.BinarySearchAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => 
                {
                    dtriple_entry.offset = (long)en.Field(2).Get();
                    object[] uni = (object[])dtriple_entry.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1) { lit.vid = LiteralVidEnumeration.integer; lit.value = (int)uni[1]; }
                    if (vid == 3) { lit.vid = LiteralVidEnumeration.date; lit.value = (long)uni[1]; }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
        }
        public IEnumerable<Literal> GetDataBySubjPred0(int subj, int pred)
        {
            return sp_d_index.GetAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en =>
                {
                    object[] uni = (object[])en.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1) { lit.vid = LiteralVidEnumeration.integer; lit.value = (int)uni[1]; }
                    if (vid == 3) { lit.vid = LiteralVidEnumeration.date; lit.value = (long)uni[1]; }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
        }
        public bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (range > 0)
            {
                int code = Scale1.Code(range, subj, pred, obj);
                //int word = (int)oscale.Root.Element(Scale1.GetArrIndex(code)).Get();
                //int tb = Scale1.GetFromWord(word, code);
                int tb = scale[code];
                if (tb == 0) return false;
                // else if (tb == 1) return true; -- это был источник ошибки
                // else надо считаль длинно, см. далее
            }
            SubjPredObjInt key = new SubjPredObjInt() { subj = subj, pred = pred, obj = obj };
            var entry = otriples.Root.BinarySearchFirst(ent => (new SubjPredObjInt(ent.Get())).CompareTo(key));
            return !entry.IsEmpty;
        }
        public bool ChkOSubjPredObj0(int subj, int pred, int obj)
        {
            // Шкалу добавлю позднее
            if (false && range > 0)
            {
                int code = Scale1.Code(range, subj, pred, obj);
                //int word = (int)oscale.Root.Element(Scale1.GetArrIndex(code)).Get();
                //int tb = Scale1.GetFromWord(word, code);
                int tb = scale[code];
                if (tb == 0) return false;
                // else if (tb == 1) return true; -- это был источник ошибки
                // else надо считаль длинно, см. далее
            }
            return !spo_o_index.GetFirst(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                cmp = pr.CompareTo(pred);
                if (cmp != 0) return cmp;
                int ob = (int)ent.Field(2).Get();
                return ob.CompareTo(obj);
            }).IsEmpty;
            //return !spo_o_index.GetFirstByKey(new SubjPredObjInt() { subj = subj, pred = pred, obj = obj }).IsEmpty;
        }
        //TODO: Надо переделать
        public XElement GetItem(string subject)
        {
            if (otriples.Root.Count() == 0) return null;
            if (dtriples.Root.Count() == 0) return null;
            XElement res = new XElement("record", new XAttribute("id", subject));
            //PaEntry dent = dtriples.Root.Element(0);
            //foreach (var ent in subject_d_index.GetAllByKey(subject))
            //foreach (var ent in sp_d_index.GetAll(en => ((string)en.Field(0).Get()).CompareTo(subject)))
            //{
            //    object[] tr = (object[])ent.Get();
            //    string predicate = (string)tr[1];
            //    object[] literal = (object[])tr[2];
            //    res.Add(new XElement("field", new XAttribute("prop", predicate),
            //        ((object[])literal[1])[0]));
            //}
            string type = null;
            //PaEntry oent = otriples.Root.Element(0);
            //foreach (var ent in subject_o_index.GetAllByKey(subject))
            foreach (var ent in spo_o_index.GetAll(en => ((string)en.Field(0).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string predicate = (string)tr[1];
                string obj = (string)tr[2];
                if (predicate == ONames.rdftypestring)
                {
                    type = obj;
                }
                else
                {
                    res.Add(new XElement("direct", new XAttribute("prop", predicate),
                        new XElement("record", new XAttribute("id", obj))));
                }
            }
            if (type != null) res.Add(new XAttribute("type", type));
            // Обратные ссылки
            //foreach (var ent in obj_o_index.GetAllByKey(subject))
            foreach (var ent in op_o_index.GetAll(en => ((string)en.Field(2).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string subj = (string)tr[0];
                string predicate = (string)tr[1];
                res.Add(new XElement("inverse", new XAttribute("prop", predicate),
                    subj));
            }

            return res;
        }

    }
}
