using System;
using System.Diagnostics;
using System.IO;

namespace CommonRDF
{
    public static class Perfomance
    {
        private static Stopwatch timer = new Stopwatch();

        /// <summary>
        /// Выводит в консоль время исполнения
        /// </summary>
        /// <param name="action">тестируемый метод</param>
        /// <param name="mesage"></param>
        /// <param name="outputFile">if true, write result at file</param>
        public static void ComputeTime(this Action action, string mesage, bool outputFile = false)
        {
            timer.Restart();
            action.Invoke();
            timer.Stop();
            if (!outputFile)
                Console.WriteLine("{0} {1}ticks", mesage, timer.Elapsed.Ticks/10000L);
            else
                using (StreamWriter file = new StreamWriter(@"D:\home\dev2012\CommonRDF\Perfomance.txt", true))
                    file.WriteLine("{0} {1}ticks", mesage, timer.Elapsed.Ticks/10000L);
        }
    }
}