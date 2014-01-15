using System;
using System.Text.RegularExpressions;

namespace CommonRDF
{

    public abstract class Triplet
    {
        public string s, p;
        /// <summary>
        /// Порождает объект класса Triplet по объектному представлению триплета 
        /// </summary>
        /// <param name="valu">Объектное представление триплета</param>
        /// <returns></returns>
        public static Triplet Create(object valu)
        {
            object[] uni = (object[])valu;
            int tag = (int)uni[0];
            object[] rec = (object[])uni[1];
            if (tag == 1) return new OProp((string)rec[0], (string)rec[1], (string)rec[2]);
            else if (tag == 2) return new DProp((string)rec[0], (string)rec[1], (string)rec[2], (string)rec[3]);
            else throw new Exception("Can't create instance of Triplet class");
        }
    }
    public class OProp : Triplet
    {
        public string o;
        public OProp(string s, string p, string o) { this.s = s; this.p = p; this.o = o; }
    }
    public class DProp : Triplet
    {
        public string d; public string lang;
        public DProp(string s, string p, string d) { this.s = s; this.p = p; this.d = d; }
        public DProp(string s, string p, string d, string l) { this.s = s; this.p = p; this.d = d; this.lang = l; }
    }

    // Четверка, получаемая из тройки (триплета). Поле vid {0|1|2} обозначает вид квада (direct, inverse, data)
    // entity - идентификатор сущности. Для нулевого и второго вариантов - это субъект, для первого - объект
    // predicate - идентификатор предиката триплета
    // rest - строка, которая либо означает сущность, дргого конца триплета (варианты 0|1) или данные (вариант 2).
    // Предполагается, что поток триплетов преобразуется в поток квадов, причем объектный триплет преобразуется в два квада, триплет с данными - в один
    public struct Quad
    {
        public Quad(int vid, string entity, string predicate, string rest)
        {
            this.vid = vid;
            this.entity = entity;
            this.predicate = predicate;
            this.rest = rest;
        }
        public int vid;
        public string entity;
        public string predicate;
        public string rest;
    }
    // В поле rest для данных - сохраняется константа и дополнительные квалификаторы. Пока поддерживается только язык
    // Для языковых спецификаторов используем синтетическую строчку вида "данные@lang". При этом @ должен находиться не далее 6 символов от конца @US-en 
    public class Axe
    {
        public string predicate;
        public string[] variants;
    }
    // Это стуктура, которая ставится в соответствие идентификатору сущности. В ней собираются все исходящие, входящие дуги и поля данных 
    public struct RecordEx
    {
        public string rtype;
        public Axe[] direct;
        public Axe[] inverse;
        public Axe[] data;
    }

    // =============== Структуры для Sparql ================
    // Переменные выстраиваются в структуру DescrVar[] в порядке появления в Sparql-конструкциях, значения динамически 
    // появляются в процессе вычислений 
    public class DescrVar
    {
        public bool isEntity = true;
        public string varName;
        public string varValue;
        public object NodeInfo;
    }
    public enum TripletVid { op, dp }
    // Все значения, появляющиеся в строчках запросов или пременные или константы. Индекс - позиция в массиве DescrVar[] 
    public class TVariable
    {
        public bool isVariable;
        public string value;
        public int index;
    }
    // Кострукция строчки запроса: вид утверждения, индекс первой еще не вычисленной переменной, остальное - очевидно
    public class Sample
    {
        public TripletVid vid;
        public int firstunknown;
        public TVariable subject, predicate, obj;
        public bool option = false;
    }

    public class FilterRegex : Sample
    {
        public readonly Regex RegularExpression;

        public FilterRegex(Regex regularExpression)
        {
            this.RegularExpression = regularExpression;
        }
    }
}
