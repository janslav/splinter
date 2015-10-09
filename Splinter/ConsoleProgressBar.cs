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
        private readonly DateTime start = DateTime.Now;

        private readonly Timer timer = new Timer(500);

        private readonly ProgressCounter progress = new ProgressCounter(0, 0, 0);

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
            DrawTextProgressBar();
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
                        Interlocked.Add(ref this.progress.skipped, t.skipped);
                        Interlocked.Add(ref this.progress.run, t.run);
                        Interlocked.Add(ref this.progress.inProgress, t.inProgress);
                        Interlocked.Add(ref this.progress.total, t.total);
                    });
            }
            else
            {
                return null;
            }
        }

        const int progressBarWidth = 20;

        private void DrawTextProgressBar()
        {
            var done = this.progress.run + this.progress.skipped;

            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = progressBarWidth + 2;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = ((float)progressBarWidth) / this.progress.total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < Math.Round(onechunk * done); i++)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw filled part
            for (int i = 0; i < Math.Round(onechunk * this.progress.inProgress); i++)
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

            var elapsed = TimeSpan.Zero;
            var estimated = TimeSpan.Zero;

            if (this.progress.total > 0 && done > 0)
            {
                elapsed = DateTime.Now - this.start;
                var percentDone = (double)done / this.progress.total;

                estimated = TimeSpan.FromTicks((long)(elapsed.Ticks / percentDone)) - elapsed;
            }

            //draw totals
            Console.CursorLeft = progressBarWidth + 5;
            Console.BackgroundColor = ConsoleColor.Black;
            //blanks at the end remove any excess
            Console.Write(
                @"{0:0000}+{1:0000}+{2:0000}/{3:0000}; elaps {4:hh\:mm\:ss}; est {5:hh\:mm\:ss}",
                this.progress.run,
                this.progress.skipped,
                this.progress.inProgress,
                this.progress.total,
                elapsed,
                estimated);
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
