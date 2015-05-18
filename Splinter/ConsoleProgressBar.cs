using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Splinter
{
    /// <summary>
    /// Displays a progress bar on the console.
    /// Stps redrawing when disposed.
    /// </summary>
    /// <typeparam name="T">The correlation object type.</typeparam>
    public class ConsoleProgressBar<T> : IDisposable
    {
        private readonly Timer timer = new Timer(500);

        private readonly ConcurrentDictionary<T, Tuple<int, int, int>> progressDict = new ConcurrentDictionary<T, Tuple<int, int, int>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleProgressBar{T}"/> class.
        /// </summary>
        public ConsoleProgressBar()
        {
            if (Environment.UserInteractive)
            {
                this.timer.Enabled = true;
                this.timer.Elapsed += OnTimerTick;
            }
        }

        void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            var values = this.progressDict.Values.ToArray();
            DrawTextProgressBar(values.Sum(v => v.Item1), values.Sum(v => v.Item2), values.Sum(v => v.Item3));
        }

        /// <summary>
        /// Creates the progress reporting object.
        /// The numbers to report are "done", "in progress" and "total"
        /// Multiple partial progressing operations can be run, those are identified by the correlation object.
        /// </summary>
        public Progress<Tuple<int, int, int>> CreateProgressReportingObject(T correlationObj)
        {
            if (Environment.UserInteractive)
            {
                return new Progress<Tuple<int, int, int>>(t => progressDict[correlationObj] = t);
            }
            else
            {
                return null;
            }
        }

        const int progressBarWidth = 50;

        private static void DrawTextProgressBar(int done, int inProgress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = progressBarWidth + 2;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = ((float)progressBarWidth) / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < Math.Round(onechunk * done); i++)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw filled part
            for (int i = 0; i < Math.Round(onechunk * inProgress); i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= progressBarWidth + 1; i++)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = progressBarWidth + 5;
            Console.BackgroundColor = ConsoleColor.Black;
            //blanks at the end remove any excess
            Console.Write("{0:0000} + {1:0000} of {2:0000} ", done, inProgress, total);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine();
            }

            this.timer.Dispose();
        }
    }
}
