using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PolarDB;
using sema2012m;

namespace CommonRDF
{
    class GraphTripletsTree : GraphBase
    {
        private readonly string path;
        private PType tp_triplets;
        private PType tp_quads;
        private PType tp_n4;
        private PType tp_graph;

        private PaCell triplets;
        private PaEntry any_triplet;

        private PxCell graph_x;
        private PxCell n4_x;

        public GraphTripletsTree(string path)
        {
            if (path[path.Length - 1] != '\\' && path[path.Length - 1] == '/') path = path + "/";
            this.path = path;
            InitTypes();
            InitCells();
        }
      
        public override void Load(params string[] rdf_files)
        {
            DateTime tt0 = DateTime.Now;
            // Закроем использование
            if (triplets != null) { triplets.Close(); triplets = null; }
            
            // Создадим ячейки
            //triplets = new PaCell(tp_triplets, path + "triplets.pac", false);
            //triplets.Clear();
            triplets = new PaCell(tp_triplets, path + "triplets.pac", false);
            triplets.Clear();

            ((ISerialFlow) triplets).StartSerialFlow();
            ((ISerialFlow) triplets).S();
            foreach (string db_falename in rdf_files)
                ReadFile(db_falename, (id, property, value, isObj, lang) =>
                    ((ISerialFlow) triplets).V(isObj
                        ? new object[] { 1, new object[] { id, property, value } }
                        : new object[] { 2, new object[] { id, property, value, lang ?? "" } }));
            ((ISerialFlow) triplets).Se();
            ((ISerialFlow) triplets).EndSerialFlow();
            Console.WriteLine("After TripletSerialInput. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            triplets.Close();
            triplets = null;
        }

        public override void CreateGraph()
        {
            if (!File.Exists(path + "triplets.pac")) //TODO throw new FileNotFoundException(path + "triplets.pac");
                return;
            // Закроем использование
            if (triplets != null) { triplets.Close(); triplets = null; }
            if (graph_x != null) { graph_x.Close(); graph_x = null; }
            if (n4_x != null) { n4_x.Close(); n4_x = null; }
            PaCell quads = null;
            PaCell graph_a = null;
            PaCell n4 = null;
            triplets = new PaCell(tp_triplets, path + "triplets.pac");

            Perfomance.ComputeTime(() =>
            {
                quads = new PaCell(tp_quads, path + "quads.pac", false);
                graph_a = new PaCell(tp_graph, path + "graph_a.pac", false);                
                graph_x = new PxCell(tp_graph, path + "graph_x.pxc", false);
                graph_x.Clear();
                n4_x = new PxCell(tp_n4, path + "n4_x.pxc", false);
                n4_x.Clear();
                n4 = new PaCell(tp_n4, path + "n4.pac", false);
                n4.Clear();
            }, "cells initiated duration=");


            Perfomance.ComputeTime(() => LoadQuads(n4, quads), "After LoadQuads(). duration=");

            // Сортировка квадриков
           Perfomance.ComputeTime( quads.Root.Sort<EntVidPredOff>," sort quards duration=");

            // Сортировка таблицы имен
            n4.Root.Sort<N4rec>();
           
            
           Perfomance.ComputeTime(()=> FormingSerialGraph(new SerialBuffer(graph_a, 3), quads),
            "Forming serial graph ok. duration=");

            // произвести объектное представление
            Perfomance.ComputeTime(()=>
            {
                graph_x.Fill2(graph_a.Root.Get().Value);
            n4_x.Fill2(n4.Root.Get().Value);
            }, "Forming fixed graph ok. duration=" );

            // ========= Завершение загрузки =========
            // Закроем файлы и уничтожим ненужные
            triplets.Close();
            quads.Close();
            File.Delete(path + "quads.pac");
            graph_a.Close();
            File.Delete(path + "graph_a.pac");
            n4.Close();
            File.Delete(path + "n4.pac");
            n4_x.Close();
            graph_x.Close();
            // Откроем для использования
            InitCells();
        }
        // ============ Технические методы ============
        private void FormingSerialGraph(ISerialFlow serial, PaCell quads)
        {
            serial.StartSerialFlow();
            serial.S();

            int hs_e = Int32.MinValue;
            int vid = Int32.MinValue;
            int vidstate = 0;
            int hs_p = Int32.MinValue;

            bool firsttime = true;
            bool firstprop = true;
            foreach (object[] el in quads.Root.Elements().Select(e => e.Value))
            {
                var record = new GraphTripletsTree.FourFields((int)el[0], (int)el[1], (int)el[2], (long)el[3]);
                if (firsttime || record.e_hs != hs_e)
                { // Начало новой записи
                    firstprop = true;
                    if (!firsttime)
                    { // Закрыть предыдущую запись
                        serial.Se();
                        serial.Re();
                        serial.Se();
                        while (vid < 2 && vidstate <= 2)
                        {
                            serial.S();
                            serial.Se();
                            vidstate += 1;
                        }
                        serial.Re();
                    }
                    vidstate = 0;
                    hs_e = record.e_hs;
                    serial.R();
                    serial.V(record.e_hs);
                    vid = record.vid;
                    while (vidstate < vid)
                    {
                        serial.S();
                        serial.Se();
                        vidstate += 1;
                    }
                    vidstate += 1;
                    serial.S();
                }
                else if (record.vid != vid)
                {
                    serial.Se();
                    serial.Re();
                    firstprop = true;

                    serial.Se();
                    vid = record.vid;
                    while (vid != vidstate)
                    {
                        serial.S();
                        serial.Se();
                        vidstate += 1;
                    }
                    vidstate += 1;
                    serial.S();
                }

                if (firstprop || record.p_hs != hs_p)
                {
                    hs_p = record.p_hs;
                    if (!firstprop)
                    {
                        serial.Se();
                        serial.Re();
                    }
                    firstprop = false;
                    serial.R();
                    serial.V(record.p_hs);
                    serial.S();
                }
                serial.V(record.off);
                firsttime = false;
            }
            if (!firsttime)
            { // Закрыть последнюю запись
                serial.Se();
                serial.Re();
                serial.Se();
                while (vid < 2 && vidstate <= 2)
                {
                    serial.S();
                    serial.Se();
                    vidstate += 1;
                }
                serial.Re();
            }
            serial.Se();
            serial.EndSerialFlow();
        }
        private struct FourFields
        {
            public readonly int e_hs;
            public readonly int vid;
            public readonly int p_hs;
            public readonly long off;
            public FourFields(int a, int b, int c, long d)
            {
                e_hs = a; vid = b; p_hs = c; off = d;
            }
        }


        private void InitCells()
        {
            string filePathTriplets = path + "triplets.pac";
            string filePathGraph = path + "graph_x.pxc";
            string filePathN4 = path + "n4_x.pxc";
            if (!File.Exists(filePathGraph) || !File.Exists(filePathN4)) return;
          
            graph_x = new PxCell(tp_graph, filePathGraph);
            n4_x=new PxCell(tp_n4, filePathN4);
            if (!File.Exists(filePathTriplets)) return;
            triplets = new PaCell(tp_triplets, filePathTriplets);
            //Console.WriteLine(triplets.Root.Count());
            any_triplet = triplets.Root.Element(0);
        }

        private delegate void QuadAction(string id, string property,
             string value, bool isObj = true, string lang = null);
        
        private static void ReadFile(string filePath, QuadAction quadAction)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == null || !File.Exists(filePath)) return;
             extension = extension.ToLower();
            if (extension == ".xml")
                ReadXML2Quad(filePath, quadAction);
            else if(extension==".nt2")
                ReadTSV(filePath,quadAction);
        }

       private static readonly Regex nsRegex = new Regex(@"^@prefix\s+(\w+):\s+<(.+)>\.$", RegexOptions.Compiled);
       private static readonly Regex tripletsRegex = new Regex("^(\\S+)\\s+(\\S+)\\s+(\"(.+)\"(@(\\S*))?|(.+))\\.$", RegexOptions.Compiled);

        static void ReadTSV(string filePath, QuadAction quadAction)
        {
            using (StreamReader reader=new StreamReader(filePath))
            {
                Match lineMatch;
                while ((lineMatch = nsRegex.Match(reader.ReadLine())).Success)
                {
                   // nsReg.Groups[1]
                    //nsReg.Groups[2]
                }
              //  var lines = new string[100000000];//100 000 000
              //  while (!reader.EndOfStream)
                int count=3000000; //000 000
                {
                    int i;
                    string readLine;
                    for (i = 0; i < count && (readLine = reader.ReadLine()) != null; i++)
                        String2Quard(quadAction, readLine);
                    //if (!GetValue(quadAction, lines[j]) && lines.Length > j + 1)
                    //    if (GetValue(quadAction, lines[j] + lines[j + 1]))
                    //        j++;
                    //    else
                    //        Console.WriteLine("unrecognized triplet {0}", lines[j]);
                }
            }
        }

        private static void String2Quard(QuadAction quadAction, string readLine)
        { 
            if (string.IsNullOrWhiteSpace(readLine))
                return;
            Match lineMatch;
            if (!(lineMatch = tripletsRegex.Match(readLine)).Success) return;
            var dMatch = lineMatch.Groups[4];
            if (dMatch.Success)
                quadAction(lineMatch.Groups[1].Value, lineMatch.Groups[2].Value, dMatch.Value, false,
                    lineMatch.Groups[6].Value);
            else
                quadAction(lineMatch.Groups[1].Value, lineMatch.Groups[2].Value, lineMatch.Groups[7].Value);
            return;
        }

        #region Read XML

        public static string langAttributeName = "xml:lang",
            rdfAbout = "rdf:about",
            rdfResource = "rdf:resource",
            NS = "http://fogid.net/o/";


        private static void ReadXML2Quad(string url, QuadAction quadAction)
        {
            string resource;
            bool isObj;
            string id = string.Empty;
            using (var xml = new XmlTextReader(url))
                while (xml.Read())
                    if (xml.IsStartElement())
                        if (xml.Depth == 1 && (id = xml[rdfAbout]) != null)
                            quadAction(id, ONames.rdftypestring, NS + xml.Name);
                        else if (xml.Depth == 2 && id != null)
                            quadAction(id, NS + xml.Name,
                                isObj: isObj = (resource = xml[rdfResource]) != null,
                                lang: isObj ? null : xml[langAttributeName],
                                value: isObj ? resource : xml.ReadString());
        }

        #endregion

        private void LoadQuads(PaCell n4, PaCell quads)
        {
            n4.StartSerialFlow();
            n4.S();
            quads.StartSerialFlow();
            quads.S();
            foreach (var tri in triplets.Root.Elements())
            {
                object[] tri_uni = (object[])tri.Value;
                int tag = (int)tri_uni[0];
                object[] rec = (object[])tri_uni[1];
                int hs_s = rec[0].GetHashCode();
                int hs_p = rec[1].GetHashCode();
                if (tag == 1) // объектое свойство
                {
                    int hs_o = rec[2].GetHashCode();
                    quads.V(new object[] { hs_s, 0, hs_p, tri.Offset });
                    quads.V(new object[] { hs_o, 1, hs_p, tri.Offset });
                }
                else // поле данных
                {
                    quads.V(new object[] { hs_s, 2, hs_p, tri.Offset });
                    if ((string)rec[1] != ONames.p_name) continue;
                    // Поместим информацию в таблицу имен n4
                    string name = (string)rec[2];
                    string name4 = name.Length <= 4 ? name : name.Substring(0, 4);
                    n4.V(new object[] { hs_s, name4.ToLower().GetHashCode() });
                }
            }
            quads.Se();
            quads.EndSerialFlow();
            n4.Se();
            n4.EndSerialFlow();
        }

        // Это нужно для сортировки квадриков, см. далее
        internal struct EntVidPredOff : IBRW, IComparable
        {
            internal int ientity, vid, ipredicate;
            internal long offset;
            internal EntVidPredOff(int ient, int vid, int ipred, long off)
            {
                this.ientity = ient;
                this.vid = vid;
                this.ipredicate = ipred;
                this.offset = off;
            }
            public IBRW BRead(BinaryReader br)
            {
                ientity = br.ReadInt32();
                vid = br.ReadInt32();
                ipredicate = br.ReadInt32();
                offset = br.ReadInt64();
                return new EntVidPredOff(ientity, vid, ipredicate, offset);
            }
            public void BWrite(BinaryWriter bw)
            {
                bw.Write(ientity);
                bw.Write(vid);
                bw.Write(ipredicate);
                bw.Write(offset);
            }
            public int CompareTo(object v2)
            {
                EntVidPredOff y = (EntVidPredOff)v2;
                int c = this.ientity.CompareTo(y.ientity); if (c != 0) return c;
                int d = this.vid.CompareTo(y.vid); if (d != 0) return d;
                return this.ipredicate.CompareTo(y.ipredicate);
            }
        }
        // Это нужно для сортировки N4, см. далее
        internal struct N4rec : IBRW, IComparable
        {
            internal int hs_e;
            internal int hs_s4;
            internal N4rec(int hs_e, int hs_s4)
            {
                this.hs_e = hs_e;
                this.hs_s4 = hs_s4;
            }
            public IBRW BRead(BinaryReader br)
            {
                hs_e = br.ReadInt32();
                hs_s4 = br.ReadInt32();
                return new N4rec(hs_e, hs_s4);
            }
            public void BWrite(BinaryWriter bw)
            {
                bw.Write(hs_e);
                bw.Write(hs_s4);
            }
            public int CompareTo(object v2)
            {
                N4rec y = (N4rec)v2;
                return this.hs_s4.CompareTo(y.hs_s4);
            }
        }

        #region Методы доступа к данным

        // =============== Методы доступа к данным ==================
        internal PxEntry GetEntryById(string id)
        {
            int e_hs = id.GetHashCode();
            PxEntry found = graph_x.Root.BinarySearchFirst(element =>
            {
                int v = (int)element.Field(0).Get().Value;
                return v < e_hs ? -1 : (v == e_hs ? 0 : 1);
            });
            return found;
        }

        internal PxEntry GetEntryByOffset(long offset)
        {
            PxEntry any = graph_x.Root.Element(0);
            any.offset = offset;
            return any;
        }
        // Нетиповой метод
        public XElement GetPortraitSimple(string id, bool showinverse)
        {
            var ent = GetEntryById(id);
            if (ent.IsEmpty) return null;
            object[] directset = (object[])ent.Field(1).Get().Value;
            object[] inverseset = showinverse ? (object[])ent.Field(2).Get().Value : null;
            object[] dataset = (object[])ent.Field(3).Get().Value;
            XElement result = new XElement("record", new XAttribute("id", id));
            foreach (object[] predseq in dataset)
            {
                object[] seq = (object[])predseq[1];
                foreach (long off in seq)
                {
                    object[] tri = (object[])GetTriplet(off);
                    object[] dp = (object[])tri[1];
                    if ((string)dp[0] != id) continue;
                    result.Add(new XElement("field", new XAttribute("prop", dp[1]),
                        string.IsNullOrEmpty((string)dp[3]) ? null : new XAttribute("lang", (string)dp[3]),
                        dp[2]));
                }
            }
            foreach (object[] predseq in directset)
            {
                object[] seq = (object[])predseq[1];
                foreach (long off in seq)
                {
                    object[] tri = (object[])GetTriplet(off);
                    object[] op = (object[])tri[1];
                    if ((string)op[0] != id) continue;
                    result.Add(new XElement("direct", new XAttribute("prop", op[1]),
                        new XElement("record", new XAttribute("id", op[2]))));
                }
            }
            if (showinverse)
            foreach (object[] predseq in inverseset)
            {
                object[] seq = (object[])predseq[1];
                foreach (long off in seq)
                {
                    object[] tri = (object[])GetTriplet(off);
                    object[] op = (object[])tri[1];
                    if ((string)op[2] != id) continue;
                    result.Add(new XElement("inverse", new XAttribute("prop", op[1]),
                        new XElement("record", new XAttribute("id", op[0]))));
                }
            }
            return result.HasElements ? result : null;
        }
        public override IEnumerable<string> GetEntities()
        {
            foreach (PxEntry pxe in graph_x.Root.Elements())
            {
                var first = pxe.Field(1).Elements().FirstOrDefault();
                if (first.Typ == null || !first.Field(1).Elements().Any())
                {
                    first = pxe.Field(2).Elements().FirstOrDefault();
                    if (first.Typ == null || !first.Field(1).Elements().Any())
                    {
                        first = pxe.Field(3).Elements().FirstOrDefault();
                        if (first.Typ == null || !first.Field(1).Elements().Any())
                            continue;
                        yield return
                            ((OProp)Triplet.Create(GetTriplet((long)first.Field(1).Elements().First().Get().Value))).o
                            ;
                    }
                }
                yield return Triplet.Create(GetTriplet((long)first.Field(1).Elements().First().Get().Value)).s;
            }
        }

        public override IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null)
        {
            return GetProperty(id, 1, t => t.s == id, nodeInfo)
                .Cast<OProp>()
                .Select(t => new PredicateEntityPair(t.p, t.o));
        }

        public override IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null)
        {
            return GetProperty(id, 2, t => t is OProp && ((OProp)t).o == id, nodeInfo)
                .Select(t => new PredicateEntityPair(t.p, t.s));
        }

        public override IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null)
        {
            return GetProperty(id, 3, t => t.s == id, nodeInfo)
                .Cast<DProp>()
                .Select(t => new PredicateDataTriple(t.p, t.d, t.d));
        }

        private IEnumerable<Triplet> GetProperty(string id, int direction, Predicate<Triplet> predicateValuesTest, object node = null, int? predicateSC = null)
        {
            //PxEntry found = GetEntryById(id);
            PxEntry found = (node is long?)
                ?  GetEntryByOffset(((long?)node).Value) : GetEntryById(id);
            if (found.IsEmpty) return Enumerable.Empty<Triplet>();
            Triplet first4Test;
            IEnumerable<PxEntry> pxEntries = found.Field(direction).Elements();
            if (predicateSC != null)
                pxEntries = pxEntries
                    .Where(pRec => (int)pRec.Field(0).Get().Value == predicateSC.Value);
            return pxEntries
                .Select(pRec =>
                    pRec.Field(1)
                        .Elements()
                        .Select(offEn => offEn.Get().Value)
                        .Cast<long>()
                        .Select(GetTriplet)
                        .Select(Triplet.Create))
                .Where(predicateResults =>
                    (first4Test = predicateResults.FirstOrDefault()) != null
                    && predicateValuesTest(first4Test))
                .SelectMany(resultsGroup => resultsGroup);
            // Еще отбраковкаtri => tri is OProp && tri.s == id &&
        }

        private object GetTriplet(long offTtripl)
        {
            any_triplet.offset = offTtripl;
            return any_triplet.Get().Value;
        }

        public override IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null)
        {
            return GetProperty(id, 1, t => t.s == id && t.p == predicate, nodeInfo, predicate.GetHashCode())
                .Cast<OProp>()
                .Select(t => t.o);
        }

        public override IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null)
        {
            return GetProperty(id, 2, t => t.p == predicate && (t is OProp) && ((OProp)t).o == id, nodeInfo, predicate.GetHashCode())
                .Select(t => t.s);
        }

        public override IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null)
        {
            return GetProperty(id, 3, t => t.s == id && t.p == predicate, nodeInfo, predicate.GetHashCode())
                .Cast<DProp>()
                .Select(t => t.d);
        }

        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null)
        {
            return GetData(id, predicate, nodeInfo).Select(SplitLang);
        }

        public override void GetItembyId(string id)
        {
            foreach (var predicateDataTriple in GetData(id))
            {
                Console.WriteLine("{0} {1} {2}", predicateDataTriple.predicate, predicateDataTriple.data,
                    predicateDataTriple.lang);
            }
        }

        public override void Test()
        {
            Console.WriteLine(string.Join(" ", SearchByName("Ершов Андрей Петрович")));
            string id = "w20070417_5_8436";
            GetItembyId(id);
            id = "piu_200809051791";
            GetItembyId(id);
        }

        public override string[] SearchByName(string ss)
        {
            return new string[0];
            //if (string.IsNullOrWhiteSpace(ss)) return new string[0];
            ////ss = ss;
            //var name4 = (ss.Length > 4 ? ss.Substring(0, 4) : ss).ToLower().GetHashCode();
            //return n4_x.Root.BinarySearchAll(
            //    e =>
            //    {
            //        int i = (int)(e.Field(1).Get().Value);
            //        return i == name4 ?0 : i < name4 ? -1 : 1;
            //    })
            //    .Select(e => e.Field(0).Get().Value)
            //    .Cast<int>()
            //    .Select(GetTriplet)
            //    .Select(Triplet.Create)
            //    .Where(t => t is DProp) // && predicate is name
            //    .Cast<DProp>()
            //    .Where(t => t.d == ss)
            //    .Select(t => t.s)
            //    .ToArray();
        }

        #region Node

        public override object GetNodeInfo(string id)
        {
         return GetEntryById(id).offset;
        }

        #endregion


        #endregion
       

        private void InitTypes()
        {
            tp_triplets =
                new PTypeSequence(
                    new PTypeUnion(
                        new NamedType("empty", new PType(PTypeEnumeration.none)), // не используется, нужен для выполнения правила атомарного варианта
                        new NamedType("op",
                            new PTypeRecord(
                                new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                                new NamedType("obj", new PType(PTypeEnumeration.sstring)))),
                        new NamedType("dp",
                            new PTypeRecord(
                                new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                                new NamedType("data", new PType(PTypeEnumeration.sstring)),
                                new NamedType("lang", new PType(PTypeEnumeration.sstring))))));
            tp_quads =
                new PTypeSequence(
                    new PTypeRecord(
                        new NamedType("hs_e", new PType(PTypeEnumeration.integer)),
                        new NamedType("vid", new PType(PTypeEnumeration.integer)),
                        new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                        new NamedType("off", new PType(PTypeEnumeration.longinteger))));
            tp_graph = new PTypeSequence(new PTypeRecord(
                new NamedType("hs_e", new PType(PTypeEnumeration.integer)),
                new NamedType("direct",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("off", new PTypeSequence(new PType(PTypeEnumeration.longinteger)))))),
                new NamedType("inverse",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("off", new PTypeSequence(new PType(PTypeEnumeration.longinteger)))))),
                new NamedType("data",
                    new PTypeSequence(
                        new PTypeRecord(
                            new NamedType("hs_p", new PType(PTypeEnumeration.integer)),
                            new NamedType("off", new PTypeSequence(new PType(PTypeEnumeration.longinteger))))))));
            tp_n4 = new PTypeSequence(new PTypeRecord(
                new NamedType("hs_e", new PType(PTypeEnumeration.integer)),
                new NamedType("s4", new PType(PTypeEnumeration.integer))));
            //tp_n4 = new PTypeSequence(new PTypeRecord(
            //    new NamedType("hs_triplets", new PType(PTypeEnumeration.longinteger)),
            //    new NamedType("s4", new PTypeFString(4))));
        }
    }
   
}