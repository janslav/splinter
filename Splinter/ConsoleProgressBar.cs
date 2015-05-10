﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Splinter
{
    public class ConsoleProgressBar<T> : IDisposable
    {
        private readonly Timer timer = new Timer(100);

        private readonly ConcurrentDictionary<T, Tuple<int, int>> progressDict = new ConcurrentDictionary<T, Tuple<int, int>>();

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
            DrawTextProgressBar(values.Sum(v => v.Item1), values.Sum(v => v.Item2));
        }

        internal Progress<Tuple<int, int>> CreateProgressReportingObject(T correlationObj)
        {
            if (Environment.UserInteractive)
            {
                return new Progress<Tuple<int, int>>(t => progressDict[correlationObj] = t);
            }
            else
            {
                return null;
            }
        }

        const int progressBarWidth = 50;

        private static void DrawTextProgressBar(int progress, int total)
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
            for (int i = 0; i < Math.Round(onechunk * progress); i++)
            {
                Console.BackgroundColor = ConsoleColor.White;
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
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }

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