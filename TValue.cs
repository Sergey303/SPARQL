using System;
using System.Linq;
using sema2012m;

namespace CommonRDF
{
    public class TValue
    {
        //public static readonly Hashtable Cach = new Hashtable();(RecordEx)(Cach[id] ?? (Cach[id] = 
        public string Value;
        public object nodeInfo;
        public bool SetNodeInfo;
        public bool? IsObject;
        private event Action<bool> whenObjSetted;
        public event Action<bool> WhenObjSetted
        {
            add
            {
                if (whenObjSetted == null) whenObjSetted = value;
                else whenObjSetted += value;
            }
            remove
            {
                whenObjSetted -= value;
            }
        }

        public void SetTargetType(bool value)
        {
            if (IsObject != null)
                if (IsObject.Value != value)
                    throw new Exception("to object sets data");
                else return;
            IsObject = value;
            if(whenObjSetted!=null)
                whenObjSetted(value);
        }

        public void SubscribeIsObjSetted(TValue connect)
        {
            Action<bool> connectOnWhenObjSetted = null;
           connectOnWhenObjSetted= 
               v=> { 
                   connect.whenObjSetted -= connectOnWhenObjSetted;
                   SetTargetType(v);
               };
            connect.WhenObjSetted += connectOnWhenObjSetted;
        }

        public void SyncIsObjectRole(TValue friend)
        {
            SubscribeIsObjSetted(friend);
            friend.SubscribeIsObjSetted(this);
        }
    }

    public abstract class SparqlBase
    {
        public abstract void Match();
        public Action NextMatch;
    }

    /// <summary>
    /// базовый класс для триплета
    /// какого класса создавать триплет определяется при чтении запроса по трём булевым переменным.
    /// </summary>
    public abstract class SparqlTriplet : SparqlBase
    {
        public TValue S, P, O;
        public bool  HasNodeInfoS, HasNodeInfoO;

        /// <summary>
        /// необходим доступ к графу, для вычисления
        /// </summary>
        public static GraphBase Gr;
        /// <summary>
        /// если известен один, то известен и другой, к сожалению у второго значение не утанавливается,
        /// и все триплеты с последним могут остаться не известными.
        /// </summary>
        public bool? IsObjectRole
        {
            get
            {
                return P.IsObject ?? (O.IsObject);
            }
            set
            {
                if (IsObjectRole != null) return;
                P.IsObject = O.IsObject = value;
            }
        }
    }
    /// <summary>
    ///  все три значения(будь то константы или параметры) не пусты
    /// при порверки данного триплета нужно проверить соттветсвие субъекта предиката объекта данным.
    /// При этомне известен вид прредиката: обектное или текстовое свойство. При константном  предикате
    /// предполагается в будущем объектность известна. Такая ситуация возможна, огда предикат параметр,
    /// встретился и был установлен ранее в запросе.
    /// </summary>
    public class SampleTriplet: SparqlTriplet
    {
        /// <summary>
        /// метод выполняет проверку: соответсвуют ли данным три значения
        /// объектность не известна, поэтому проверить нужно и текстовые и объектные предикаты
        /// субъекта на соответствие с указанным значением предиката.
        /// </summary>
        /// <returns>возвращает тоже, что и следующий Match, если триплет соответсвует данным, ложь если нет.</returns>
        public override void Match()
        {
            object nodeInfo = HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value));
            if (IsObjectRole==null)
                if (Gr.GetDirect(S.Value, P.Value, nodeInfo).Any(oValue => oValue == O.Value))
                {
                    IsObjectRole = true;
                 NextMatch();
                }
                else if (Gr.GetData(S.Value, P.Value, nodeInfo).Any(oValue => oValue == O.Value))
                {
                    IsObjectRole = false;
                    NextMatch();
                }
                else return;
            else if ((IsObjectRole.Value
                    ? Gr.GetDirect(S.Value, P.Value, nodeInfo)
                    : Gr.GetData(S.Value, P.Value, nodeInfo))
                    .Any(oValue => oValue == O.Value))
                    NextMatch();
           
        }
    }
    /// <summary>
    /// этот триплет создаёттся в ситуаци, когда не известен только субъект на данном этапе выполнения. 
    /// </summary>
    public class SelectSubject : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override void Match()
        {
            if (IsObjectRole==null || IsObjectRole.Value)
                foreach (string value in Gr.GetInverse(O.Value, P.Value, HasNodeInfoO ? O.nodeInfo : (O.nodeInfo = Gr.GetNodeInfo(O.Value))))
                {
                    IsObjectRole = true;
                    S.Value = value;
                    NextMatch();
                }
            if (IsObjectRole!=null && IsObjectRole.Value) return;
            foreach (string value in
                P.Value == ONames.p_name
                    ? Gr.SearchByName(O.Value).Where(OnPredicate)
                    : Gr.GetEntities().Where(OnPredicate))
            {
                IsObjectRole = false;
                S.Value = value;
                NextMatch();
            }
        }
        protected bool OnPredicate(string id)
        {
            return Gr.GetData(id, P.Value).Contains(O.Value);
        }
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObject : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override void Match()
        {
            if (IsObjectRole==null|| IsObjectRole.Value)
                foreach (string value in Gr.GetDirect(S.Value, P.Value, HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value))))
                {
                    IsObjectRole = true;
                    O.Value = value;
                    NextMatch();
                }
            if (IsObjectRole!=null && IsObjectRole.Value) return;
            foreach (string value in Gr.GetData(S.Value, P.Value, HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value))))
            {
                IsObjectRole = false;
                O.Value = value;
                NextMatch();
            }
        }
    }
    /// <summary>
    /// аналогично двум предыдущим
    /// </summary>
    public class SelectPredicate : SparqlTriplet
    {
        /// <summary>
        /// метод находит все предикаты, известного субъекта, с известным значением, и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override void Match()
        {
            throw new NotImplementedException();
        }
    }
    public class SelectAllPredicatesBySub :SparqlTriplet{
        public override void Match()
        {
            throw new NotImplementedException();
        }
    }
    public class SelectAllSubjects : SparqlTriplet
    
    {
        public override void Match()
        {
            foreach (var id in Gr.GetEntities())
            {
                S.Value = id;
                NextMatch();
            }
        }
    }

    public class SelectSubjectOpional : SelectSubject
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов субъектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override void Match()
        {
            bool any = false;
            if (IsObjectRole==null || IsObjectRole.Value)
                foreach (string value in Gr.GetInverse(O.Value, P.Value, HasNodeInfoO ? O.nodeInfo : (O.nodeInfo = Gr.GetNodeInfo(O.Value))))
                {
                    IsObjectRole = true;
                    any = true;
                    S.Value = value;
                    NextMatch();
                }
            if (any) return;
            if (IsObjectRole == null || !IsObjectRole.Value)
                foreach (string value in
                    P.Value == ONames.p_name
                        ? Gr.SearchByName(O.Value).Where(OnPredicate)
                        : Gr.GetEntities().Where(OnPredicate))
                {
                    IsObjectRole = false;
                    any = true;
                    S.Value = value;
                    NextMatch();
                }
            if (any) return;
            S.Value = string.Empty;
            NextMatch();
        }
    }

    /// <summary>
    /// этот триплет создаётся в ситуаци, когда не известен только объект на данном этапе выполнения. 
    /// </summary>
    public class SelectObjectOprtional : SparqlTriplet
    {
        /// <summary>
        /// метод выполняет перебор всех вариантов объектов и для каждого запускает следующий Match
        /// </summary>
        /// <returns></returns>
        public override void Match()
        {
            if (IsObjectRole==null || IsObjectRole.Value)
                foreach (string value in Gr.GetDirect(S.Value, P.Value,
                            HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value))))
                {
                    IsObjectRole = true;
                    any = true;
                    O.Value = value;
                    NextMatch();
                }
            if (any) return;
            if (IsObjectRole == null || !IsObjectRole.Value)
            foreach (string value in Gr.GetData(S.Value, P.Value,
                        HasNodeInfoS ? S.nodeInfo : (S.nodeInfo = Gr.GetNodeInfo(S.Value))))
            {
                IsObjectRole = false;
                any = true;
                O.Value = value;
                NextMatch();
            }
            if (any) return;
            O.Value = string.Empty;
            NextMatch();
        }

        private bool any;
    }
    public class SelectAllSubjectsOptional : SparqlTriplet
    {
        private bool any;
        public override void Match()
        {
            foreach (var id in Gr.GetEntities())
            {
                any = true;
                S.Value = id;
                NextMatch();
            }
            if (any) return;
            S.Value = string.Empty;
            NextMatch();
        }
    }
}
