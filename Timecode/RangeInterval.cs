using System;
using System.Collections.Generic;
using System.Text;

namespace TimecodeUtils.Timecode
{
    public class RangeInterval
    {
        public int StartFrame;
        public int EndFrame;
        public double Interval;

        public RangeInterval(int startFrame, int endFrame, double interval)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
            Interval = interval;
        }

        public RangeInterval(RangeInterval rangeInterval)
        {
            StartFrame = rangeInterval.StartFrame;
            EndFrame = rangeInterval.EndFrame;
            Interval = rangeInterval.Interval;
        }
    }
}
