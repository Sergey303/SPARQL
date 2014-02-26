using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparql
{
    internal enum Tokens
    {
        error = 1, EOF = 2, csKeyword = 3, csIdent = 4, csNumber = 5, csLitstr = 6,
        csVerbstr = 7, csLitchr = 8, csOp = 9, csBar = 10, csDot = 11, semi = 12,
        csStar = 13, csLT = 14, csGT = 15, comma = 16, csSlash = 17, csLBrac = 18,
        csRBrac = 19, csLPar = 20, csRPar = 21, csLBrace = 22, csRBrace = 23, verbatim = 24,
        pattern = 25, name = 26, lCond = 27, rCond = 28, lxLBrace = 29, lxRBrace = 30,
        lxBar = 31, defCommentS = 32, defCommentE = 33, csCommentS = 34, csCommentE = 35, usingTag = 36,
        namespaceTag = 37, optionTag = 38, charSetPredTag = 39, inclTag = 40, exclTag = 41, lPcBrace = 42,
        rPcBrace = 43, visibilityTag = 44, PCPC = 45, userCharPredTag = 46, scanbaseTag = 47, tokentypeTag = 48,
        scannertypeTag = 49, lxIndent = 50, lxEndIndent = 51, maxParseToken = 52, EOL = 53, csCommentL = 54,
        errTok = 55, repErr = 56
    };

    public abstract class ScanBase
    {
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yylex")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yylex")]
        public abstract int yylex();

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "yywrap")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "yywrap")]
        protected virtual bool yywrap() { return true; }

#if BABEL
        protected abstract int CurrentSc { get; set; }
        // EolState is the 32-bit of state data persisted at 
        // the end of each line for Visual Studio colorization.  
        // The default is to return CurrentSc.  You must override
        // this if you want more complicated behavior.
        public virtual int EolState { 
            get { return CurrentSc; }
            set { CurrentSc = value; } 
        }
    }
    
     internal interface IColorScan
    {
        void SetSource(string source, int offset);
        int GetNext(ref int state, out int start, out int end);
#endif // BABEL
    }
}
