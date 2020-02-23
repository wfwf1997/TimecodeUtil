/*
 * Copyright (C) 2020 Mark Wu. All rights reserved.
 * Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace TimecodeUtil.Timecode
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
