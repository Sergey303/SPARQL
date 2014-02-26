
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrueRdfViewer;

namespace Sparql
{
    public enum LiteralVidEnumeration { unknown, integer, text, date }
    public class Literal
    {
        public LiteralVidEnumeration vid;
        public object value;
        public override string ToString()
        {
            switch (vid)
            {
                case LiteralVidEnumeration.text:
                    {
                        Text txt = (Text)value;
                        return "\"" + txt.s + "\"@" + txt.l;
                    }
                default: return value.ToString();
            }
        }
    }
    public class Text { public string s, l; }
             
    public struct GraphSelectorAndParams
        {
            public Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> GraphSelector;
            public List<short> Parameters;
        }
    public class Parser
    {
        private Scanner scanner;
        private readonly Dictionary<string, string> prefixes = new Dictionary<string, string>();
        private Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>> where;
        public Parser(Scanner scanner)
        {
            this.scanner = scanner;
        }
            private readonly Dictionary<string, object> variables=new Dictionary<string, object>(),
                constants=new Dictionary<string, object>();


        internal string ReplaceNamespacePrefix( string value)
        {
            var nsO = value.Split(':');
            string nsUri;
            if (!prefixes.TryGetValue(nsO[0].Trim(), out nsUri))
                throw new Exception("неизвестное пространство имён " + nsO[0]);
           return nsUri + nsO[1].Trim();
            
        }

        protected bool TestParameter(Scanner.QueryLiteral literal , out object spo)
        {
            spo = 0;
            switch (literal.Type)
            {
                case Scanner.QueryLiteralType.Var:
                    if (variables.TryGetValue(literal.Value, out spo))
                        return false; //TODO ReferenceEquals(spo.value, "hasParellellValue");
                    variables.Add(literal.Value, spo = new Literal() {});
                    return true;
                case Scanner.QueryLiteralType.IRIref:
                    literal.Value = literal.Value.Substring(1, literal.Value.Length - 2);    //remove < >
                    if (constants.TryGetValue(literal.Value, out spo))
                        constants.Add(literal.Value, spo = TripleInt.Code(literal.Value));
                    return false;
                case Scanner.QueryLiteralType.PrefixedName:
                    literal.Value = ReplaceNamespacePrefix(literal.Value);
                    if (constants.TryGetValue(literal.Value, out spo))
                        constants.Add(literal.Value, spo = TripleInt.Code(literal.Value));
                    return false;
            }
            return false;
        }

        protected Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> CreateTriplet(Scanner.QueryLiteral sValue, Scanner.QueryLiteral pValue, Scanner.QueryLiteral oValue,
            bool isOptional)
        {
            object s, p, o;
            bool isData = true;    
            bool isNewS = TestParameter(sValue, out s);
            bool isNewP = TestParameter(pValue, out p);
            bool isNewO = TestParameter(oValue, out o);
            return null;
        }

        
        public GraphSelectorAndParams  Parse()
        {
            var f = new List<Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>>>();
            var parametersAdded=new List<short>();
            while (scanner.QueryLiteralsWhere.Count>0)
            {
              var literal=  scanner.QueryLiteralsWhere.Dequeue();
                if (literal.Type == Scanner.QueryLiteralType.Triplet)
                     f.Add(CreateTriplet((Scanner.Triplet) literal));
                else
                if (literal.Type == Scanner.QueryLiteralType.Optional)
                {
                    if(scanner.QueryLiteralsWhere.Dequeue().Type!=Scanner.QueryLiteralType.OpenBrac) throw new Exception("optional must starts by open brace");
                    var subGraphSelectorAndParams = Parse();
                    parametersAdded.AddRange(subGraphSelectorAndParams.Parameters);
                    f.Add(subGraphSelectorAndParams.Optional());
                }
                if (literal.Type == Scanner.QueryLiteralType.OpenBrac)
                {
                    var subGraphSelectorAndParams = Parse();
                    Scanner.QueryLiteral possibleUnion = scanner.QueryLiteralsWhere.Peek();
                    if (possibleUnion.Type == Scanner.QueryLiteralType.Union)
                    {
                        var subSelectors = new List<GraphSelectorAndParams>();
                        while (possibleUnion.Type == Scanner.QueryLiteralType.Union)
                        {
                            scanner.QueryLiteralsWhere.Dequeue(); // "union"
                            if (scanner.QueryLiteralsWhere.Dequeue().Type != Scanner.QueryLiteralType.OpenBrac)
                                throw new Exception("union must continues by open brace");
                            subSelectors.Add(Parse());
                            possibleUnion = scanner.QueryLiteralsWhere.Peek();
                        }
                        f.Add();
                    }
                    else
                    {
                        parametersAdded.AddRange(subGraphSelectorAndParams.Parameters);
                        f.Add(subGraphSelectorAndParams.GraphSelector);
                    }
                }
                else
                if (literal.Type == Scanner.QueryLiteralType.CloseBrac) break;
            }
            return new GraphSelectorAndParams
            {
                GraphSelector = (Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>>) Delegate.Combine(f.ToArray()), //TODO reimplement Combine for list
                Parameters = parametersAdded
            };
        }

        private Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> CreateTriplet(Scanner.Triplet literal)
        {
            return null;
        }
    }
    //public class Parser
    //{
    //    readonly Queue<QueryLiteral> queryLiterals=new Queue<QueryLiteral>();
    //   static readonly Regex unSpace=new Regex(@"\S*", RegexOptions.Singleline), spaces=new Regex(@"\s*", RegexOptions.Singleline);

    //    public Parser(string queryString)
    //    {
    //        QueryLevel level=QueryLevel.Main;
    //        var queryChars = queryString.ToCharArray();
    //        int braceLevel = 0;
    //        for (int i = 0; i < queryChars.Length-1; i++)
    //        {
    //            if (queryChars[i] != ' ' && queryChars[i] != '\n' && queryChars[i] != '\r' && queryChars[i] != '\t')
    //            {
    //                if (queryChars[i + 1] == ' ' || queryChars[i + 1] == '\n' || queryChars[i + 1] == '\r' ||
    //                    queryChars[i + 1] == '\t')
    //                {
    //                    switch (queryChars[i])
    //                    {
    //                        case 'a': break;
    //                    }
    //                }
    //                var word = unSpace.Match(queryString, i);
    //                //int j = i+1;
    //                    //while (queryChars[++j]!=' '){}
    //                    //queryLiterals.Enqueue(new QueryLiteral(queryString.Substring(i, j)));
    //                if (queryChars[i] == '?')
    //                {
    //                    queryLiterals.Enqueue(new QueryLiteral(word.Value, QueryLiteralType.variable));
    //                    i += word.Length;
    //                }
    //                else
    //                switch (level)
    //                {
    //                    case QueryLevel.Main:
    //                    {
    //                        switch (queryChars[i])
    //                        {
    //                            case 'S':
    //                            case 's':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Select));
    //                                i += word.Length;
    //                                break;
    //                            case 'P':
    //                            case 'p':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Prefix));
    //                                i += word.Length;
    //                                i += spaces.Match(queryString, i).Length;
    //                                word = unSpace.Match(queryString, i);
    //                                queryLiterals.Enqueue(new QueryLiteral(word.Value,QueryLiteralType.PrefixNsName));
    //                                i += word.Length;
    //                                i += spaces.Match(queryString, i).Length;
    //                                word = unSpace.Match(queryString, i);
    //                                queryLiterals.Enqueue(new QueryLiteral(word.Value,QueryLiteralType.prefixNsUri));
    //                                i += word.Length;
    //                                break;
    //                            case 'O':
    //                            case 'o':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.OrderBy));
    //                                i += word.Length;
    //                                break;
    //                            case 'L':
    //                            case 'l':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Limit));
    //                                i += word.Length;
    //                                break;
    //                            case 'W':
    //                            case 'w':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Where));
    //                                i += word.Length;
    //                                i = WaitChar(i, queryChars, '{');
    //                                braceLevel = 1;
    //                                level = QueryLevel.InsideWhere;
    //                                break;
    //                        }
    //                    }
    //                    break;
    //                    case QueryLevel.InsideWhere:
    //                    {
    //                        switch (queryChars[i])
    //                        {
    //                            case 'o':
    //                            case 'O':
    //                                if (word.Value.ToUpper() == "OPTIONAL")
    //                                {
    //                                    queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Optional));
    //                                    i = WaitChar(i + word.Length, queryChars, '{');
    //                                    braceLevel++;
    //                                }
    //                                break;
    //                            case 'f':
    //                            case 'F':
    //                                if (word.Value.ToUpper() == "FILTER")
    //                                {
    //                                    queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Filter));
    //                                    i += word.Length;
    //                                }
    //                                break;
    //                            case '\'':
    //                            case '"':
    //                                queryLiterals.Enqueue(new QueryLiteral(word.Value, QueryLiteralType.Text));
    //                                i += word.Length;
    //                                break;
    //                            case '.':
    //                                break;
    //                            case '(':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.OpenBrack));
    //                                break;
    //                            case ')':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.CloseBrack));
    //                                break;
    //                            case '{':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.OpenBrac));
    //                                braceLevel++;
    //                                break;
    //                            case '}':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.CloseBrac));
    //                                if(braceLevel--==0) level=QueryLevel.Main;
    //                                break;
    //                            case '<':
    //                                if (word.Value == "<")
    //                                    queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Smaller));
    //                                else if (word.Value == "<=")
    //                                {
    //                                    queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.SmallerOrEqual));
    //                                    i += 1;
    //                                }
    //                                else
    //                                {
    //                                    queryLiterals.Enqueue(new QueryLiteral(word.Value, QueryLiteralType.Iri));
    //                                    i += word.Length;
    //                                }
    //                                break;
    //                            case '=':
    //                                if (word.Value == "=")
    //                                    queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Equal));
    //                                else if (word.Value == "=>")
    //                                {
    //                                    queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.BigOrEqual));
    //                                    i += 1;
    //                                }
    //                                break;
    //                            case '>':
    //                                queryLiterals.Enqueue(new QueryLiteral(QueryLiteralType.Biger));
    //                                break;
    //                            default:
    //                                queryLiterals.Enqueue(new QueryLiteral(word.Value, QueryLiteralType.IriShort));
    //                                i += word.Length;
    //                                break;
    //                        }
    //                    }
    //                        break;
    //                    default:
    //                        throw new ArgumentOutOfRangeException();
    //                }
    //            }
    //        }
    //    }

    //    private int WaitChar(int i, char[] queryChars, char c)
    //    {
    //        while (queryChars[i] != c) i++;
    //        return i;
    //    }
    //}
}
