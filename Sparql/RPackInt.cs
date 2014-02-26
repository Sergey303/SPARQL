using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public class RPackInt
    {
        //public bool result;
        public object[] row;
        private TripleStoreInt ts;
        public TripleStoreInt Store { get { return ts; } }
        public RPackInt(object[] row, TripleStoreInt ts)
        {
            this.row = row;
            this.ts = ts;
        }
        public object Get(object si)
        {
            return si is short ? row[(int)si] : si;
        }
        public int GetE(object si)
        {
            return si is short ? (int)row[(short)si] : (int)si;
        }
        public Literal Val(int ind)
        {
            return (Literal)row[ind];
        }
        public int Vai(int ind)
        {
            Literal lit = (Literal)row[ind];
            if (lit.vid != LiteralVidEnumeration.integer) throw new Exception("Wrong literal vid in Vai method");
            return (int)lit.value;
        }
        public void Set(object si, object valu)
        {
            if (!(si is short)) throw new Exception("argument must be an index");
            short ind = (short)si;
            row[ind] = valu;
        }
    }
    public static class RPackExtentionInt
    {
        // для следующих методов subj, pred, obj или short индекс или целое значение закодированного Entity
        public static IEnumerable<RPackInt> spo(this IEnumerable<RPackInt> pack, object subj, object pred, object obj)
        {
            //subj = P(subj); pred = P(pred); obj = P(obj);
            return pack.Where(pk => pk.Store.ChkOSubjPredObj(pk.GetE(subj), pk.GetE(pred), pk.GetE(obj)));
        }
        public static IEnumerable<RPackInt> Spo(this IEnumerable<RPackInt> pack, object subj, object pred, object obj)
        {
            if (!(subj is short)) throw new Exception("subject must be an index");
            //pred = P(pred); obj = P(obj);
            return pack.SelectMany(pk => pk.Store
                .GetSubjectByObjPred(pk.GetE(obj), pk.GetE(pred))
                .Select(su =>
                {
                    pk.Set(subj, su);
                    return new RPackInt(pk.row, pk.Store);
                }));
        }
        public static IEnumerable<RPackInt> spO(this IEnumerable<RPackInt> pack, object subj, object pred, object obj)
        {
            if (!(obj is short)) throw new Exception("object must be an index");
            //subj = P(subj); pred = P(pred);
            return pack.SelectMany(pk => pk.Store
                .GetObjBySubjPred(pk.GetE(subj), pk.GetE(pred))
                .Select(ob =>
                {
                    pk.Set(obj, ob);
                    return new RPackInt(pk.row, pk.Store);
                }));
        }
        public static IEnumerable<RPackInt> spD(this IEnumerable<RPackInt> pack, object subj, object pred, object dat)
        {
            if (!(dat is short)) throw new Exception("data must be an index");
            //subj = P(subj); pred = P(pred);
            return pack.SelectMany(pk => pk.Store
                .GetDataBySubjPred(pk.GetE(subj), pk.GetE(pred))
                .Select(da =>
                {
                    pk.Set(dat, da); //((Text)da.value).s);
                    return new RPackInt(pk.row, pk.Store);
                }));
        }
    }
}