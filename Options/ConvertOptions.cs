/*
 * Copyright (C) 2020 Mark Wu. All rights reserved.
 * Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
 */
using TimecodeUtil.Timecode;

namespace TimecodeUtil.Options
{
    class ConvertOptions
    {
        public TimecodeVersion Version { get; set; } = TimecodeVersion.V2;
        public string Output { get; set; } = string.Empty;
        public double? Fps;
    }
}
