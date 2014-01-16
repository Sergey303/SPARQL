using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using sema2012m;

namespace CommonRDF
{
    public interface IReceiver
    {
        void Restart();
        void Receive(string[] row);
    }
    public class SimpleSparql
    {
        private Sample[] testquery = new Sample[0];
        private DescrVar[] testvars = new DescrVar[0];
        public SimpleSparql(string id)
        {

            testquery = TestQueryBSBM_1();
            testvars = descrVarsBSBM_1;
        }

        public bool Match(GraphBase gr, IReceiver receive)
        {
            Match(gr, 0, receive);
            return Success;
        }

        public bool Success;

        private static DescrVar[] DescrVarsPA(string id)
        {
            return new DescrVar[]
            {
                new DescrVar {isEntity = true, varName = "?s"},
                new DescrVar {isEntity = true, varName = "?inorg"},
                new DescrVar {isEntity = false, varName = "?orgname"},
                new DescrVar {isEntity = false, varName = "?fd"},
                //consts objects
                new DescrVar {isEntity = true, varValue = id},
                new DescrVar {isEntity = true, varValue = "http://fogid.net/o/participation"},
            };
        }

        private static Sample[] TestQueryPA(string id)
        {
            return new Sample[]
            {
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 0,
                    subject = new TVariable {isVariable = true, value = "?s", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.p_participant},
                    obj = new TVariable {isVariable = false, value = id, index = 4}
                },
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 1,
                    subject = new TVariable {isVariable = true, value = "?s", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.p_inorg},
                    obj = new TVariable {isVariable = true, value = "?inorg", index = 1}
                },
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 2,
                    subject = new TVariable {isVariable = true, value = id, index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.rdftypestring},
                    obj = new TVariable {isVariable = false, value = "http://fogid.net/o/participation", index = 5}
                },
                new Sample
                {
                    vid = TripletVid.dp,
                    firstunknown = 2,
                    subject = new TVariable {isVariable = true, value = "?inorg", index = 1},
                    predicate = new TVariable {isVariable = false, value = ONames.p_name},
                    obj = new TVariable {isVariable = true, value = "?orgname", index = 2}
                },
                new Sample
                {
                    vid = TripletVid.dp,
                    firstunknown = 3,
                    subject = new TVariable {isVariable = true, value = "?s", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.p_fromdate},
                    obj = new TVariable {isVariable = true, value = "?fd", index = 3},
                    option = true
                },
                new FilterRegex(new Regex("^$", RegexOptions.Compiled | RegexOptions.CultureInvariant))
                {
                    subject = new TVariable {isVariable = true, value = "?fd", index = 3}
                }
            };
        }
        private static DescrVar[] descrVarsBSBM_1 = new DescrVar[] 
            {
                new DescrVar { isEntity = true, varName="?product" },
                new DescrVar { isEntity = true, varName="http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductType15" },
                new DescrVar { isEntity = true, varName="http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature80" },
                new DescrVar { isEntity = true, varName="http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature102" },
                new DescrVar { isEntity = false, varValue ="?value1" },
                new DescrVar { isEntity = false, varValue ="?label" },
            };
        private static Sample[] TestQueryBSBM_1()
        {
            return new Sample[]
            {
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 0,
                    subject = new TVariable {isVariable = true, value = "?product", index = 0},
                    predicate = new TVariable {isVariable = false, value = ONames.rdftypestring},
                    obj = new TVariable {isVariable = false, value = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductType15", index = 1}
                },
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 1,                 
                    subject = new TVariable {isVariable = true, value = "?product", index = 0},
                    predicate = new TVariable {isVariable = false, value = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature"},
                    obj = new TVariable {isVariable = false, value = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature80", index = 2}
                },
                new Sample
                {
                    vid = TripletVid.op,
                    firstunknown = 2,
                    subject = new TVariable {isVariable = true, value = "?product", index = 0},                 
                    predicate = new TVariable {isVariable = false, value = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature"},
                    obj = new TVariable {isVariable = false, value = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature102", index = 3}
                },
                new Sample
                {
                    vid = TripletVid.dp,
                    firstunknown = 2,
                    subject = new TVariable {isVariable = true, value = "?product", index = 0},
                    predicate = new TVariable {isVariable = false, value ="http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productPropertyNumeric1"},
                    obj = new TVariable {isVariable = true, value = "?value1", index = 4}
                },
                new Sample
                {
                    vid = TripletVid.dp,
                    firstunknown = 3,
                    subject = new TVariable {isVariable = true, value = "?product", index = 0},
                    predicate = new TVariable {isVariable = false, value = "http://www.w3.org/2000/01/rdf-schema#label" },
                    obj = new TVariable {isVariable = true, value = "?label", index = 5},
                },
                new FilterSample()
                {
                    Expression = vars => vars[3].IsDouble && vars[3].DoubleValue > 1000
                }
            };
        }

        // Возвращает истину если сопоставление состоялось хотя бы один раз
        private void Match(GraphBase gr, int nextsample, IReceiver receive)
        {
            // Вывести если дошли до конца
            if (nextsample >= testquery.Length)
            {
                string[] row = new string[testvars.Length];
                for ( int i = 0; i < testvars.Length; i++)
                {
                    row[i] = testvars[i].varValue;
                }
                receive.Receive(row);
                Success = true;
                //Console.Write("R:"); // Здесь будет вывод значения переменных
                //foreach (var va in testvars)
                //{
                //    Console.Write(va.varName + "=" + va.varValue + " ");
                //}
                //Console.WriteLine();
                return;
            }
            // Match
            var sam = testquery[nextsample];
            // Разметить пустыми значениями, если option
            if (sam.option)
            {
                if (sam.firstunknown < testvars.Length) testvars[sam.firstunknown].varValue = null;
            }
            if (sam is FilterRegex)
            {
                var filter = ((FilterRegex) sam).RegularExpression.Match(testvars[sam.subject.index].varValue);
                if (filter.Success)
                    Match(gr, nextsample + 1, receive);
                return;
            }
            // Пока считаю предикаты известными. Вариантов 4: 0 - обе части неизвестны, 1 - субъект известен, 2 - объект известен, 3 - все известно
            int variant = (sam.subject.isVariable && sam.subject.index >= sam.firstunknown ? 0 : 1) +
                (sam.obj.isVariable && sam.obj.index >= sam.firstunknown ? 0 : 2);
            bool atleastonce = false;
            if (variant == 1)
            {
                string idd = sam.subject.isVariable ? testvars[sam.subject.index].varValue : sam.subject.value;
                object nodeInfo = testvars[sam.subject.index].NodeInfo ??
                                  (testvars[sam.subject.index].NodeInfo = gr.GetNodeInfo(idd));
                // В зависимости от вида, будут использоваться данные разных осей
                if (sam.vid == TripletVid.dp)
                { // Dataproperty
                    foreach (var data in gr.GetData(idd, sam.predicate.value, nodeInfo))
                    {
                        testvars[sam.obj.index].varValue = data;
                        atleastonce = true;
                        Match(gr, nextsample + 1, receive);
                    }
                }
                else
                {
                    // Objectproperty
                    foreach (var directid in gr.GetDirect(idd, sam.predicate.value, nodeInfo))
                    {
                        atleastonce = true;
                        testvars[sam.obj.index].varValue = directid;
                        testvars[sam.obj.index].NodeInfo = null;
                        Match(gr, nextsample + 1, receive);
                    }
                }
                if(atleastonce || !sam.option) return;
                testvars[sam.obj.index].varValue = string.Empty;
                  Match(gr, nextsample + 1, receive);
            }
            else if (variant == 2) // obj - known, subj - unknown
            {
                string ido = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                // Пока будем обрабатывать только объектные ссылки
                if (sam.vid == TripletVid.op)
                {
                    object nodeInfo = testvars[sam.obj.index].NodeInfo ??
                                  (testvars[sam.obj.index].NodeInfo = gr.GetNodeInfo(ido));
                    foreach (var inverseid in gr.GetInverse(ido, sam.predicate.value, nodeInfo))
                    {
                        testvars[sam.subject.index].varValue = inverseid;
                        testvars[sam.subject.index].NodeInfo = null;
                        Match(gr, nextsample + 1, receive);
                    }
                    //TODO: Нужен ли вариант с опцией?
                }
                else
                { //Здесь вариант, когда данное известно
                    if (sam.predicate.value==ONames.p_name)
                        foreach (var id in gr.SearchByName(ido))
                        {
                            atleastonce = true;
                            testvars[sam.subject.index].varValue = id;
                            testvars[sam.subject.index].NodeInfo = null;
                            Match(gr, nextsample + 1, receive);
                        }
                    else
                    foreach (var id in gr.GetEntities().Where(id => gr.GetData(id, sam.predicate.value).Contains(ido)))
                    {
                        atleastonce = true;
                        testvars[sam.subject.index].varValue = id;
                        testvars[sam.subject.index].NodeInfo = null;
                        Match(gr, nextsample + 1, receive);
                    }
                }
                if (atleastonce || !sam.option) return;
                testvars[sam.obj.index].varValue = string.Empty;
                Match(gr, nextsample + 1, receive);
            }
            else if (variant == 3)
            {
                if (sam.option) { Match(gr, nextsample + 1, receive); return;} //TODO: Нужен ли вариант, связанный с опциями?
                string idd = sam.subject.isVariable ? testvars[sam.subject.index].varValue : sam.subject.value;
                foreach (var directid in 
                    gr.GetDirect(idd, sam.predicate.value, 
                        testvars[sam.subject.index].NodeInfo ?? 
                    (testvars[sam.subject.index].NodeInfo = gr.GetNodeInfo(idd))))
                {
                    string objvalue = sam.obj.isVariable ? testvars[sam.obj.index].varValue : sam.obj.value;
                    if (objvalue != directid) continue;
                    Match(gr, nextsample + 1, receive);
                }
            }
            else
            {
                throw new Exception("Unimplemented");
            }
        }
    }
}
