using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PolarBasedRDF;
using PolarDB;
using sema2012m;

namespace CommonRDF
{

    public class PolarBasedRdfGraph : GraphBase
    {
   private readonly PType ptDirects = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("o", new PType(PTypeEnumeration.sstring))
            ));

        private readonly PType ptData = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("d", new PType(PTypeEnumeration.sstring)),
            new NamedType("l", new PType(PTypeEnumeration.sstring))
            ));

        private PaCell directCell, dataCell;
        private readonly string directCellPath;
        private readonly string dataCellPath;
        private FixedIndex<string> sDirectIndex, oIndex, sDataIndex;
        private FixedIndex<SubjPred> spDirectIndex, opIndex, spDataIndex;

        public PolarBasedRdfGraph(DirectoryInfo path)
        {
            this.path = path.FullName;
            if (!path.Exists) path.Create();
            directCell = new PaCell(ptDirects, directCellPath = Path.Combine(path.FullName, "rdf.direct.pac"),
                File.Exists(directCellPath));
            dataCell = new PaCell(ptData, dataCellPath = Path.Combine(path.FullName, "rdf.data.pac"),
                File.Exists(dataCellPath));
            if (dataCell.IsEmpty || directCell.IsEmpty) return;
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            if(sDataIndex!=null)
            {
                throw new Exception("xgasrhase");
            }
            sDataIndex = new FixedIndex<string>(path + "s of data", dataCell.Root, entry => (string) entry.Field(0).Get());
            sDirectIndex = new FixedIndex<string>(path + "s of direct", directCell.Root, entry => (string)entry.Field(0).Get());
            oIndex = new FixedIndex<string>(path + "o of direct", directCell.Root, entry => (string)entry.Field(2).Get());
            spDataIndex = new FixedIndex<SubjPred>(path + "s and p of data", dataCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(0).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            spDirectIndex = new FixedIndex<SubjPred>(path + "s and p of direct", directCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(0).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            opIndex = new FixedIndex<SubjPred>("o and p of direct", directCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(2).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
        }

        public void Load(int tripletsCountLimit, params string[] filesPaths)
        {
            directCell.Close();
            dataCell.Close();

            File.Delete(dataCellPath);
            File.Delete(directCellPath);

            directCell = new PaCell(ptDirects, directCellPath, false);
            dataCell = new PaCell(ptData, dataCellPath, false);

            var directSerialFlow = (ISerialFlow) directCell;
            var dataSerialFlow = (ISerialFlow) dataCell;
            directSerialFlow.StartSerialFlow();
            dataSerialFlow.StartSerialFlow();
            directSerialFlow.S();
            dataSerialFlow.S();
            ReaderRDF.ReaderRDF.ReadFiles(tripletsCountLimit, filesPaths, (id, property, value, isObj, lang) =>
                {
                    if (isObj)
                        directSerialFlow.V(new object[] {id, property, value});
                    else dataSerialFlow.V(new object[] {id, property, value, lang ?? ""});
                });
            directSerialFlow.Se();
            dataSerialFlow.Se();
            directSerialFlow.EndSerialFlow();
            dataSerialFlow.EndSerialFlow();
        }

        internal void LoadIndexes()
        {
            sDataIndex.Load(null);
            sDirectIndex.Load(null);
            oIndex.Load(null);
            var subjPredComparer = new SubjPredComparer();
            spDataIndex.Load(subjPredComparer);
            spDirectIndex.Load(subjPredComparer);
            opIndex.Load(subjPredComparer);
        }


        public string GetItem(string id)
        {
            return
                sDirectIndex.GetAllByKey(id)
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Concat(
                        oIndex.GetAllByKey(id)
                            .Select(spo => spo.Type.Interpret(spo.Get()))
                            .Concat(
                                sDataIndex.GetAllByKey(id)
                                    .Select(spo => spo.Type.Interpret(spo.Get()))))
                    .Aggregate((all, one) => all + one);
        }

        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            var type =
                spDirectIndex.GetFirstByKey(new SubjPred {pred = ONames.rdftypestring, subj = id});
            XElement res = new XElement("record", new XAttribute("id", id),
                type.offset == long.MinValue ? null : new XAttribute("type", ((object[]) type.Get())[2]),
                sDataIndex.GetAllByKey(id).Select(entry => entry.Get()).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", v3[1]),
                        string.IsNullOrEmpty((string) v3[3]) ? null : new XAttribute(ONames.xmllang, v3[3]),
                        v3[2])),
                sDirectIndex.GetAllByKey(id).Select(entry => entry.Get()).Cast<object[]>().Select(v2 =>
                    new XElement("direct", new XAttribute("prop", v2[1]),
                        new XElement("record", new XAttribute("id", v2[2])))),
                null);
            // Обратные ссылки
            if (addinverse)
            {
                var query = oIndex.GetAllByKey(id);
                string predicate = null;
                XElement inverse = null;
                foreach (PaEntry en in query)
                {
                    var rec = (object[]) en.Get();
                    string pred = (string) rec[1];
                    if (pred != predicate)
                    {
                        res.Add(inverse);
                        inverse = new XElement("inverse", new XAttribute("prop", pred));
                        predicate = pred;
                    }
                    string idd = (string) rec[0];
                    inverse.Add(new XElement("record", new XAttribute("id", idd)));
                }
                res.Add(inverse);
            }
            return res;
        }

        public override void Load(params string[] rdfFiles)
        {
            Load(Int32.MaxValue, rdfFiles);
            CreateIndexes();
            LoadIndexes();
        }

        public override void CreateGraph()
        {
        //  CreateIndexes();
        }

        public override IEnumerable<string> GetEntities()
        {
            var existed = new HashSet<string>();
            foreach (var id in dataCell.Root.Elements().Select(entry => entry.Field(0).Get()).Cast<string>().Where(s => !existed.Contains(s)))
            {
                existed.Add(id);
                yield return id;
            }
        }

        public override IEnumerable<PredicateEntityPair> GetDirect(string id, object nodeInfo = null)
        {
            return sDirectIndex.GetAllByKey(id).Select(entry => new PredicateEntityPair((string)entry.Field(1).Get(), (string)entry.Field(2).Get()));
        }

        readonly Dictionary<string, IEnumerable<PredicateEntityPair>> cacheDirect = new Dictionary<string, IEnumerable<PredicateEntityPair>>(),
                                                                      cacheInverse=new Dictionary<string, IEnumerable<PredicateEntityPair>>();
        readonly Dictionary<string, IEnumerable<PredicateDataTriple>> cacheData = new Dictionary<string, IEnumerable<PredicateDataTriple>>();
        readonly Dictionary<string, IEnumerable<DataLangPair>> cacheDataLangPredicate = new Dictionary<string, IEnumerable<DataLangPair>>();
        readonly Dictionary<string, IEnumerable<string>> cacheDataPredicate = new Dictionary<string, IEnumerable<string>>(),
                                                         cacheDirectPredicate=new Dictionary<string, IEnumerable<string>>(),
                                                         cacheInversePredicate=new Dictionary<string, IEnumerable<string>>();

        private readonly string path;

        public override IEnumerable<PredicateEntityPair> GetInverse(string id, object nodeInfo = null)
        {
            IEnumerable<PredicateEntityPair> cached;
            if (cacheDirect.TryGetValue(id, out cached)) return cached;

            cacheInverse.Add(id,
                cached =
                    sDirectIndex.GetAllByKey(id)
                        .Select(entry => new PredicateEntityPair((string) entry.Field(1).Get(), (string) entry.Field(0).Get())));
            return cached;
        }

        public override IEnumerable<PredicateDataTriple> GetData(string id, object nodeInfo = null)
        {
            IEnumerable<PredicateDataTriple> cached;
            if (cacheData.TryGetValue(id, out cached)) return cached;
            cacheData.Add(id,
                cached =
                    sDataIndex.GetAllByKey(id)
                        .Select(
                            entry =>
                                new PredicateDataTriple((string) entry.Field(1).Get(), (string) entry.Field(2).Get(),
                                    (string) entry.Field(3).Get())));
            return cached;
        }

        public override IEnumerable<string> GetDirect(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheDirectPredicate.TryGetValue(id+predicate, out cached)) return cached;
            cacheDirectPredicate.Add(id + predicate,
                cached =
                    spDirectIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry => (string) entry.Field(2).Get()));
            return cached;
        }

        public override IEnumerable<string> GetInverse(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheInversePredicate.TryGetValue(id + predicate, out cached)) return cached;
            cacheInversePredicate.Add(id + predicate,
                cached =
                    opIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry => (string)entry.Field(0).Get()));
            return cached;
        }

        public override IEnumerable<string> GetData(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<string> cached;
            if (cacheDataPredicate.TryGetValue(id + predicate, out cached)) return cached;
            cacheDataPredicate.Add(id + predicate,
                cached =
                    spDataIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry =>(string)entry.Field(2).Get()));
            return cached;
        }

        public override IEnumerable<DataLangPair> GetDataLangPairs(string id, string predicate, object nodeInfo = null)
        {
            IEnumerable<DataLangPair> cached;
            if (cacheDataLangPredicate.TryGetValue(id, out cached)) return cached;
            cacheDataLangPredicate.Add(id,
                cached =
                    spDataIndex.GetAllByKey(new SubjPred(id, predicate))
                        .Select(entry => new DataLangPair((string)entry.Field(2).Get(), (string)entry.Field(3).Get())));
            return cached;
        }

        public override void GetItembyId(string id)
        {
            throw new NotImplementedException();
        }

        public override void Test()
        {
            string testId = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature13";
            var testDAta = GetData(testId);
            var pred = testDAta.First().predicate;
            var testDAtaPred = GetData(testId, pred);
            var testDataLang = GetDataLangPairs(testId, pred);
            var testInverse = GetInverse(testId);
            var inverseFirst = testInverse.FirstOrDefault();
            if(Equals(inverseFirst, default(PredicateEntityPair))) return;
            var testInversePredicate = GetInverse(testId, inverseFirst.predicate);

        }

        public override string[] SearchByName(string ss)
        {
            throw new NotImplementedException();
        }

        public override object GetNodeInfo(string id)
        {
            return id;
        }
    }
}
