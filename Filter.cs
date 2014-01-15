using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CommonRDF
    {
        class SparqlFilterRegex : SparqlBase
        {
            private static readonly Regex RegFilteRregex = new Regex(@"^(\?\w+), "+"\"(.*?)\""+@"(,\s*"+"\"(?<flags>[ismx]*)?\")*$", RegexOptions.Compiled);
            public TValue Parameter;
            public readonly string ParameterName;
            private readonly Regex regularExpression;

            public SparqlFilterRegex(string parameterExpressionFlags)
            {
                var regMatch = RegFilteRregex.Match(parameterExpressionFlags);
               ParameterName = regMatch.Groups[1].Value;
                var flagsMatch = regMatch.Groups["flags"];
                RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                if (flagsMatch.Success)
                {
                    if (flagsMatch.Value.Contains("i"))
                        options = options | RegexOptions.IgnoreCase;

                    if (flagsMatch.Value.Contains("s"))
                        options = options | RegexOptions.Singleline;

                    if (flagsMatch.Value.Contains("m"))
                        options = options | RegexOptions.Multiline;
                    if (flagsMatch.Value.Contains("x"))
                        options = options | RegexOptions.IgnorePatternWhitespace;
                }
                regularExpression = new Regex(regMatch.Groups[2].Value, options);
            }
            public override void Match()
            {
                if (regularExpression.Match(Parameter.Value).Success)
                    NextMatch();
            }
        }
        
    class FilterItem : SparqlBase
    {
      
        /// <summary>
        /// FILTER regex(?title, "^SPARQL")
        /// FILTER (?v < 3)
        /// </summary>
        public string ExpressionString;
        /// <summary>
        /// Dynamicly created function.
        /// Run Filter on parametrs.
        /// </summary>
        public Func<Dictionary<string, string>, bool> Test;


        public FilterItem()
        {
            
        }
        
        private static readonly Regex regFILTER = new Regex("FILTER \\((?<regExpression>w+)\\)", RegexOptions.Compiled);
        private static readonly Regex regParentes = new Regex("\\((?<inside>[^)^(])\\)", RegexOptions.Compiled);
        private static readonly Regex RegSum = new Regex("(?<left>[^+])\\+(?<right>[.])", RegexOptions.Compiled);
        private static readonly Regex RegDiff = new Regex("(?<left>[^-])\\-(?<right>[.])", RegexOptions.Compiled);
        private static readonly Regex RegMul = new Regex("(?<left>[^*])\\*(?<right>[.])", RegexOptions.Compiled);
        private static readonly Regex RegDiv = new Regex("(?<left>[^/])\\/(?<right>[.])", RegexOptions.Compiled);
        private static readonly Regex RegMoreThen = new Regex("(?<left>[^<=])<s*=(?<right>[.])", RegexOptions.Compiled);
        
        public bool IsFilter(string s)
        {
            return regFILTER.Match(s).Success;
        }
        public static Dictionary<string, Func<Dictionary<string, string>, bool>> FuncStore = new Dictionary<string, Func<Dictionary<string, string>, bool>>();
       
        Func<Dictionary<string, string>, bool> Fdynamic2Fbool(Func<Dictionary<string, string>, dynamic> f)
        {
            return arg =>(bool)f(arg);
        }

        //public void Convert()
        //{
        //    var match = RegFilteRregex.Match(ExpressionString);
        //    if(match.Success)
        //    {
        //        var parameter = match.Groups["paramter"].Value;
        //        var regExpression = match.Groups["regExpression"].Value;
        //        var ismx = match.Groups["ps"] != null ? match.Groups["ps"].Value : "";
        //        Test = parameters => Regex(parameters[parameter], regExpression, ismx);
        //    }            
        //}

        
       
        public static bool LangMatches(string languageTag, string languageRange)
        {
            if (languageRange == "*") return languageTag != string.Empty;
            return languageTag.ToLower().Contains(languageRange.ToLower());
        }

        public static bool SameTerm(string termLeft, string termRight)
        {            
            var leftSubStrings=termLeft.Split(new[]{"^^"}, StringSplitOptions.RemoveEmptyEntries);
            var rightSubStrings=termRight.Split(new[]{"^^"}, StringSplitOptions.RemoveEmptyEntries);
            if (leftSubStrings.Length == 2 && rightSubStrings.Length == 2)
            {      
                //different types
                if (!String.Equals(rightSubStrings[1], leftSubStrings[1], StringComparison.CurrentCultureIgnoreCase)) return false;                
                ///TODO: different namespaces and same types
            }
                double leftDouble, rightDouble;
                DateTime leftDate, rightDate;
                return rightSubStrings[0]==leftSubStrings[0]
                       ||
                       (Double.TryParse(leftSubStrings[0].Replace("\"", ""), out leftDouble)
                                      &&
                        Double.TryParse(rightSubStrings[0].Replace("\"", ""), out rightDouble)
                                      &&
                        leftDouble == rightDouble)
                        ||
                        (DateTime.TryParse(leftSubStrings[0].Replace("\"", ""), out leftDate)
                                      &&
                        DateTime.TryParse(rightSubStrings[0].Replace("\"", ""), out rightDate)
                                      &&
                        leftDate == rightDate);                    
        }
        
        public static string Lang(string term)
        {
            var substrings = term.Split('@');
            if (substrings.Length == 2)
                return substrings[1];
            return string.Empty;
        }


        public override void Match()
        {
            throw new NotImplementedException();
        }
    }
       public static class Extensions
       {
           public static Func<Tin, Tout> Cache<Tin,Tout>(this Func<Tin,Tout> f)
           {
               Dictionary<Tin, Tout> cache = new Dictionary<Tin, Tout>();
               return p =>
               {
                   Tout r;
                   if (!cache.TryGetValue(p, out r))
                       cache.Add(p, r = f(p));
                   return r;

               };
           }
       }
    }
