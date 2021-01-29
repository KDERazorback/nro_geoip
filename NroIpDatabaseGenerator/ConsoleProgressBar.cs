using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator
{
    internal class ConsoleProgressBar
    {
        protected long ValueMember;
        protected int MaxBarSegments;
        protected int CurrentBarSegments;
        protected long MaximumMember;

        public long Maximum
        {
            get
            {
                return MaximumMember;
            }
            set
            {
                MaximumMember = value;
                UpdateLayout();

                int newSegmentCount = (int)((Percent / MaxBarSegments) * 100.0f);
                if (newSegmentCount != CurrentBarSegments)
                    NeedsRedraw = true;

                CurrentBarSegments = newSegmentCount;
            }
        }
        public long Value
        {
            get
            {
                return ValueMember;
            }
            set
            {
                ValueMember = value;

                int newSegmentCount = (int)((Percent / MaxBarSegments) * 100.0f);
                if (newSegmentCount != CurrentBarSegments)
                    NeedsRedraw = true;

                CurrentBarSegments = newSegmentCount;
            }
        }
        public bool Enabled { get; set; }
        public float Percent => Math.Clamp((float)Value / Maximum, 0, 1) * 100.0f;
        public int AvailableWindowWidth => Enabled ? Math.Min(100, Console.BufferWidth - 9) : 0;
        public bool NeedsRedraw { get; protected set; }
        public char ProgressChar { get; set; } = '=';
        public void Refresh()
        {
            NeedsRedraw = false;

            if (!Enabled)
                return;

            Console.CursorLeft = 0;
            Console.Write('[');
            Console.Write(new string(ProgressChar, CurrentBarSegments));
            Console.Write(new string(' ', MaxBarSegments - CurrentBarSegments));
            Console.Write("] ");
            if (Percent < 100)
                Console.Write(' ');
            if (Percent < 10)
                Console.Write(' ');
            Console.Write(Percent.ToString("N0"));
            Console.Write(" %");
        }
        public void Clear()
        {
            if (!Enabled)
                return;

            Console.CursorLeft = 0;
            Console.Write(new string(' ', AvailableWindowWidth));
            Console.CursorLeft = 0;
        }

        public void UpdateLayout()
        {
            if (!Enabled)
                return;

            MaxBarSegments = (int)Math.Round((float)Maximum / (int)(Maximum / AvailableWindowWidth), 0);
        }

    }
}
