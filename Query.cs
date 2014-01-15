using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sema2012m;

namespace CommonRDF
{
    internal class Query
    {
        public static Regex QuerySelectReg = new Regex(@"[Ss][Ee][Ll][Ee][Cc][Tt]\s+((\?\w+\s+)+|\*)",
            RegexOptions.Compiled);

        public static Regex QueryWhereReg = new Regex(@"[Ww][Hh][Ee][Rr][Ee]\s+\{(([^{}]*\{[^{}]*\}[^{}]*)*|[^{}]*)\}",
            RegexOptions.Compiled);

        public static Regex TripletsReg = new Regex(
            //@"((?<s>[^\s]+|'.*')\s+(?<p>[^\s]+|'.*')\s+(?<o>[^\s]+|'.*')\.(\s|$))|([Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll]\s+{\s*(?<os>[^\s]+|'.*')\s+(?<op>[^\s]+|'.*')\s+(?<oo>[^\s]+|'.*')\s*}(\s|$))|[Ff][Ii][Ll][Tt][Ee][Rr]\s+(?<filterttype>[^\s()]+)?\((?<filter>.*)\)"
            @"(([^\s]+|'.*')\s+([^\s]+|'.*')\s+([^\s]+|'.*')\.(\s|$))|([Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll]\s+{\s*([^\s]+|'.*')\s+([^\s]+|'.*')\s+([^\s]+|'.*')\s*}(\s|$))|[Ff][Ii][Ll][Tt][Ee][Rr]\s+([^\s()]+)?\((.*)\)"
            );

        public GraphBase Gr;
       // public List<QueryTripletOptional> Optionals;
        // public TValue[] Parameters;
        public string[] ParametersNames;
        private readonly TValue[] parameters;
        public TValue[] ParametersWithMultiValues;
        public readonly List<string> SelectParameters=new List<string>();
        public readonly List<string[]> ParametrsValuesList = new List<string[]>();
        private readonly SparqlBase start;
        #region Read

        public Query(StreamReader stream, GraphBase graph):this(stream.ReadToEnd(), graph) { }
        public Query(string sparqlString, GraphBase graph)
        {
            var valuesByName = new Dictionary<string, TValue>();

            var selectMatch = QuerySelectReg.Match(sparqlString);
            if (selectMatch.Success)
            {
                string parameters2Select = selectMatch.Groups[1].Value.Trim();
                if (parameters2Select != "*")
                    SelectParameters =
                        parameters2Select.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
               sparqlString = sparqlString.Replace(selectMatch.Groups[0].Value, "");
            }
            var whereMatch = QueryWhereReg.Match(sparqlString);
            if (whereMatch.Success)
            {
                string tripletsGroup = whereMatch.Groups[1].Value;
                SparqlBase newTriplet = null, lastTriplet = null;
                SparqlTriplet.Gr = Gr = graph;
                foreach (Match tripletMatch in TripletsReg.Matches(tripletsGroup))
                {
                    var sMatch = tripletMatch.Groups[2];
                    string pValue;
                    string oValue;
                    if (sMatch.Success)
                    {
                        pValue = tripletMatch.Groups[3].Value;
                        oValue = tripletMatch.Groups[4].Value;
                        if (CreateTriplet(sMatch, valuesByName, pValue, oValue, false, ref newTriplet)) continue;
                    }
                    else if ((sMatch = tripletMatch.Groups[7]).Success)
                    {
                        pValue = tripletMatch.Groups[8].Value;
                        oValue = tripletMatch.Groups[9].Value;
                        if (CreateTriplet(sMatch, valuesByName, pValue, oValue, true, ref newTriplet)) continue;
                    }
                    else if ((sMatch = tripletMatch.Groups[12]).Success)
                    {
                        var filter = sMatch.Value;
                        var filterType = tripletMatch.Groups[11].Value.ToLower();
                        if (filterType == "regex")
                        {
                            var newFilter = new SparqlFilterRegex(filter);
                            if (!valuesByName.TryGetValue(newFilter.ParameterName, out newFilter.Parameter))
                            {
                                valuesByName.Add(newFilter.ParameterName, newFilter.Parameter = new TValue());
                                throw new NotImplementedException("new parameter in fiter regex");
                            }
                            newTriplet = newFilter;
                        }
                    }
                    else throw new Exception("strange query triplet: " + tripletMatch.Value);



                    if (lastTriplet != null)
                        lastTriplet.NextMatch = newTriplet.Match;
                    else start = newTriplet;
                    lastTriplet = newTriplet;
                }
                if (lastTriplet != null)
                    lastTriplet.NextMatch = Match;
            }
          //  sparqlString = sparqlString.Replace(whereMatch.Groups[0].Value, "");
            //QueryTriplet.Gr = Gr;
            //QueryTriplet.Match = Match;
            //QueryTripletOptional.Gr = Gr;
            //QueryTripletOptional.MatchOptional = MatchOptional;
            parameters = valuesByName.Values.Where(v => v.Value == null).ToArray();
            ParametersNames = valuesByName.Where(v => v.Value.Value == null).Select(kv => kv.Key).ToArray();
        }

        private static bool CreateTriplet(Group sMatch, Dictionary<string, TValue> valuesByName, string pValue, string oValue, bool isOptional,
            ref SparqlBase newTriplet)
        {
            bool isData;
            TValue s, p, o;
            string sValue = sMatch.Value.TrimStart('<').TrimEnd('>');
            bool isNewS = TestParameter(sValue,
                out s, valuesByName);
            bool isNewP = TestParameter(pValue = pValue.TrimStart('<').TrimEnd('>'),
                out p, valuesByName);
            bool isNewO = TestParameter(oValue = (isData = oValue.StartsWith("'"))
                ? oValue.Trim('\'')
                : oValue.TrimStart('<').TrimEnd('>'), out o, valuesByName);

            s.SetTargetType(true);
            if (isData)
            {
                o.SetTargetType(false);
                p.SetTargetType(false);
            }
            else if (!isNewO)
            {
                o.SetTargetType(true);
                p.SetTargetType(true);
            }
            else
                p.SyncIsObjectRole(o);
            if (!isNewP)
                if (!isNewS)
                {
                    if (!isNewO)
                    {
                        if (isOptional) return true;
                        newTriplet = new SampleTriplet
                        {
                            S = s,
                            P = p,
                            O = o,
                            HasNodeInfoS = s.SetNodeInfo
                        };
                    }
                    else
                        newTriplet = isOptional
                            ? (SparqlTriplet) new SelectObjectOprtional()
                            {
                                S = s,
                                P = p,
                                O = o,
                                HasNodeInfoS = s.SetNodeInfo
                            }
                            : new SelectObject
                            {
                                S = s,
                                P = p,
                                O = o,
                                HasNodeInfoS = s.SetNodeInfo
                            };

                    s.SetNodeInfo = true;
                }
                else if (!isNewO)
                {
                    newTriplet = isOptional
                        ? new SelectSubjectOpional
                        {
                            S = s,
                            P = p,
                            O = o,
                            HasNodeInfoO = o.SetNodeInfo
                        }
                        : new SelectSubject
                        {
                            S = s,
                            P = p,
                            O = o,
                            HasNodeInfoO = o.SetNodeInfo
                        };
                    o.SetNodeInfo = true;
                }
                else
                {
                    newTriplet = isOptional
                        ? (SparqlTriplet) new SelectAllSubjectsOptional
                        {
                            S = s,
                            P = p,
                            O = o,
                            NextMatch = () => new SelectObjectOprtional
                            {
                                S = s,
                                P = p,
                                O = o,
                                HasNodeInfoS = s.SetNodeInfo
                            }.Match()
                        }
                        : new SelectAllSubjects
                        {
                            S = s,
                            P = p,
                            O = o,
                            NextMatch = () => new SelectObject
                            {
                                S = s,
                                P = p,
                                O = o,
                                HasNodeInfoS = s.SetNodeInfo
                            }.Match()
                        };
                    s.SetNodeInfo = true;
                }
            else if (!isNewS)
                if (!isNewO) newTriplet = new SelectPredicate();
                else
                {
                    //Action = SelectPredicateObject;
                }
            else if (!isNewO)
            {
                //Action = SelectPredicateSubject;
            }
            else
            {
                //Action = SelectAll;
            }
            return false;
        }

        private static bool TestParameter(string spoValue, out TValue spo, Dictionary<string, TValue> paramByName)
        {
            if (!spoValue.StartsWith("?"))
            {
                if (!paramByName.TryGetValue(spoValue, out spo))
                    paramByName.Add(spoValue, spo = new TValue { Value = spoValue });
            }
            else
            {
                if (paramByName.TryGetValue(spoValue, out spo))
                    return false;
                paramByName.Add(spoValue, spo = new TValue());
                return true;
            }
            return false;
        }

        #endregion


        #region Run

        public void Run()
        {
            if(start==null) return;
                start.Match();
        }


        private void Match()
        {
            ParametrsValuesList.Add(parameters.Select(par => par.Value).ToArray());
        }

        #endregion
        
        #region Output in file

        internal void OutputParamsAll(string outPath)
        {
            using (var io = new StreamWriter(outPath, true))
                foreach (var parametrsValues in ParametrsValuesList)
                {
                    for (int i = 0; i < parametrsValues.Length; i++)
                    {
                        io.WriteLine("{0} {1}", ParametersNames[i], parametrsValues[i]);
                    }
                    io.WriteLine();
                }
        }

        internal void OutputParamsBySelect(string outPath)
        {
            var parametrsValuesIndexes = ParametersNames
                .Select((e, i) => new { e, i });
            using (var io = new StreamWriter(outPath, true, Encoding.UTF8))
                foreach (var parametrsValues in ParametrsValuesList)
                {
                    foreach (var i in SelectParameters
                        .Select(p => parametrsValuesIndexes.First(e => e.e == p)))
                    {
                        io.WriteLine("{0}", parametrsValues[i.i]);
                    }
                    io.WriteLine();
                }
        }

        #endregion
    }
}
