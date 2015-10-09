using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Splinter
{
    using System.Threading;

    using Timer = System.Timers.Timer;

    public class ProgressCounter
    {
        public ProgressCounter(int run = 0, int inProgress = 0, int skipped = 0, int total = 0)
        {
            this.run = run;
            this.inProgress = inProgress;
            this.total = total;
            this.skipped = skipped;
        }

        public int skipped;

        public int run;

        public int inProgress;

        public int total;
    }

    /// <summary>
    /// Displays a progress bar on the console. This is a stateful class.
    /// Stops redrawing when disposed.
    /// </summary>
    public class ConsoleProgressBar : IDisposable
    {
        private readonly Timer timer = new Timer(500);

        private ProgressCounter progress = new ProgressCounter(0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleProgressBar"/> class.
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
            var values = this.progress;
            DrawTextProgressBar(values.run + values.skipped, values.inProgress, values.total);
        }

        /// <summary>
        /// Creates the progress reporting object.
        /// </summary>
        public Progress<ProgressCounter> CreateProgressReportingObject()
        {
            if (Environment.UserInteractive)
            {
                return new Progress<ProgressCounter>(
                    t =>
                    {
                        Interlocked.Add(ref progress.skipped, t.skipped);
                        Interlocked.Add(ref progress.run, t.run);
                        Interlocked.Add(ref progress.inProgress, t.inProgress);
                        Interlocked.Add(ref progress.total, t.total);
                    });
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
