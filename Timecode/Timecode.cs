using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace TimecodeUtils.Timecode
{
    public class Timecode
    {
        private readonly List<RangeInterval> _intervalList = new List<RangeInterval>();

        /// <summary>
        /// Enumerable copies of range intervals
        /// </summary>
        public IEnumerable<RangeInterval> IntervalList => _intervalList.Select(i => new RangeInterval(i));

        /// <summary>
        /// Total length of the timecode file
        /// </summary>
        public TimeSpan TotalLength =>
            new TimeSpan((long) _intervalList.Sum(interval =>
                interval.Interval * (interval.EndFrame - interval.StartFrame + 1)));

        /// <summary>
        /// Total frames of the timecode file
        /// </summary>
        public int TotalFrames => _intervalList.Count == 0 ? 0 : _intervalList[^1].EndFrame + 1;

        /// <summary>
        /// Average frame rate of the timecode file
        /// </summary>
        public double AverageFrameRate => 1e7 * TotalFrames / TotalLength.Ticks;

        /// <summary>
        /// Calculated default frame rate of the timecode
        /// </summary>
        public double DefaultFrameRate => _intervalList.Count == 0
            ? 0
            : 1e7 / _intervalList.GroupBy(i => i.Interval)
                .OrderByDescending(g => g.Count())
                .First().Key;

        /// <summary>
        /// Original version of the input timecode
        /// </summary>
        public readonly TimecodeVersion Version;

        /// <summary>
        /// The constructor. It reads timecode info from specific file
        /// </summary>
        /// <param name="path">The path of the timecode file being loaded. It can either be in v1 or v2 format</param>
        /// <param name="frames">The total frames of the timecode file for fill the "gaps".
        /// This parameter only effected when reading timecode v1. If not provided, the last frame from the last record is used</param>
        public Timecode(string path, int frames = 0)
            : this(new FileStream(path, FileMode.Open, FileAccess.Read), frames)
        {
        }

        /// <summary>
        /// The constructor. It reads timecode info from specific file
        /// </summary>
        /// <param name="stream">The stream represents the timecode file being loaded. It can either be in v1 or v2 format</param>
        /// <param name="frames">The total frames of the timecode file for fill the "gaps".
        /// This parameter only effected when reading timecode v1. If not provided, the last frame from the last record is used</param>
        public Timecode(Stream stream, int frames = 0)
        {
            using var reader = new StreamReader(stream);

            var line = reader.ReadLine() ?? throw new InvalidOperationException();
            var regex = new Regex(@"\A# time(?:code|stamp) format (v[1-2])");
            var match = regex.Match(line);
            if (!match.Success)
            {
                throw new FormatException("Illegal file header or timecode version.");
            }

            switch (match.Groups[1].Value)
            {
                case "v1":
                    Version = TimecodeVersion.V1;
                    TimecodeV1Handler(reader, frames);
                    break;
                case "v2":
                    Version = TimecodeVersion.V2;
                    TimecodeV2Handler(reader);
                    break;
            }

            NormalizeInterval();
        }

        /// <summary>
        /// Function to the timecode file.
        /// </summary>
        /// <param name="path">The location for timecode file being generated</param>
        /// <param name="version">The version of timecode file</param>
        public void SaveTimecode(string path, TimecodeVersion version = TimecodeVersion.V2)
        {
            using var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            SaveTimecode(fileStream, version);
        }

        /// <summary>
        /// Function to the timecode file.
        /// </summary>
        /// <param name="path">The stream for outputing timecode</param>
        /// <param name="version">The version of timecode file</param>
        public void SaveTimecode(Stream stream, TimecodeVersion version = TimecodeVersion.V2)
        {
            using var writer = new StreamWriter(stream);

            switch (version)
            {
                case TimecodeVersion.V1:
                    SaveTimecodeV1(writer);
                    break;
                case TimecodeVersion.V2:
                    SaveTimecodeV2(writer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, null);
            }
        }

        /// <summary>
        /// Get frame number from a given time span
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public int GetFrameNumberFromTimeSpan(TimeSpan ts)
        {
            double time = 0;
            var tick = ts.Ticks;
            foreach (var interval in _intervalList)
            {
                time += interval.Interval * (interval.EndFrame - interval.StartFrame + 1);
                if (time < tick) continue;
                var deltaFrame = (int) Math.Round((time - tick) / interval.Interval);
                return interval.EndFrame - deltaFrame + 1;
            }

            return _intervalList[^1].EndFrame;
        }

        /// <summary>
        /// Get time span from a given frame number
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public TimeSpan GetTimeSpanFromFrameNumber(int frame)
        {
            double time = 0;
            foreach (var interval in _intervalList)
            {
                time += interval.Interval * (interval.EndFrame - interval.StartFrame + 1);
                if (interval.EndFrame < frame) continue;
                var deltaTime = interval.Interval * (interval.EndFrame - frame + 1);
                return new TimeSpan((long) Math.Round(time - deltaTime));
            }

            return new TimeSpan((long) Math.Round(time));
        }

        private void TimecodeV1Handler(TextReader reader, int frames)
        {
            double defaultInterval = 0;
            var intervals = new List<RangeInterval>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                if (!line.StartsWith("assume", true, null)) continue;
                defaultInterval = 1e7 / double.Parse(line.Substring(6));
                break;
            }

            if (defaultInterval == 0) throw new FormatException("Can not find default frame rate description.");

            var lineRegex = new Regex(@"^(?<start>\d+)\s*,\s*(?<end>\d+)\s*,\s*(?<rate>\d+(?:\.\d*)?)$");
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var match = lineRegex.Match(line);
                if (!match.Success) throw new FormatException($"Illegal line: {line}");
                var start = int.Parse(match.Groups["start"].Value);
                var end = int.Parse(match.Groups["end"].Value);
                var inter = 1e7 / double.Parse(match.Groups["rate"].Value);
                if (start > end)
                    throw new FormatException($"The start frame {start} is greater than the end frame {end}");
                intervals.Add(new RangeInterval(start, end, inter));
            }

            intervals.Sort((a, b) => a.StartFrame - b.StartFrame);
            var lastStart = -1;
            var lastEnd = -1;
            foreach (var interval in intervals)
            {
                if (interval.StartFrame <= lastEnd)
                {
                    _intervalList.Clear();
                    throw new FormatException(
                        $"Frame range {lastStart}-{lastEnd} and {interval.StartFrame}-{interval.EndFrame} overlapped");
                }

                if (interval.StartFrame - lastEnd > 1)
                {
                    _intervalList.Add(new RangeInterval(lastEnd + 1,
                        interval.StartFrame - 1, defaultInterval));
                }

                _intervalList.Add(interval);

                lastStart = interval.StartFrame;
                lastEnd = interval.EndFrame;
            }

            if (frames > TotalFrames)
            {
                _intervalList.Add(new RangeInterval(TotalFrames, frames - 1, defaultInterval));
            }
        }

        private void TimecodeV2Handler(TextReader reader)
        {
            double currentTime;
            double lastTime = -1;
            double lastDiff = 0;
            double firstTime = 0;
            var firstFrame = 0;
            var currentFrame = -1;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                currentTime = double.Parse(line);
                ++currentFrame;
                if (lastTime == -1)
                {
                    lastTime = currentTime;
                    continue;
                }

                if (currentTime == lastTime)
                {
                    if (currentFrame != 1)
                        throw new FormatException(
                            $"Frame {currentFrame - 1} and {currentFrame} is displayed at the same time");
                    lastTime = currentTime;
                    continue;
                }

                if (Math.Abs(currentTime - lastTime - lastDiff) < 1e-3 || lastDiff == 0)
                {
                    lastDiff = currentTime - lastTime;
                    lastTime = currentTime;
                    continue;
                }

                _intervalList.Add(new RangeInterval(firstFrame, currentFrame - 2,
                    1e4 * (lastTime - firstTime) / (currentFrame - firstFrame - 1)));
                firstFrame = currentFrame - 1;
                firstTime = lastTime;
                lastDiff = currentTime - lastTime;
                lastTime = currentTime;
            }

            _intervalList.Add(new RangeInterval(firstFrame, currentFrame,
                (1e4 * (lastTime - firstTime) / (currentFrame - firstFrame))));
        }

        private void NormalizeInterval()
        {
            foreach (var interval in _intervalList)
            {
                if (Math.Abs(Math.Round(1001e4 / interval.Interval) - 1001e4 / interval.Interval) < 1e-6)
                {
                    interval.Interval = 1001e4 / Math.Round(1001e4 / interval.Interval);
                }
                else if (Math.Abs(Math.Round(1000e4 / interval.Interval) - 1000e4 / interval.Interval) < 1e-6)
                {
                    interval.Interval = 1000e4 / Math.Round(1000e4 / interval.Interval);
                }
            }
        }

        private void SaveTimecodeV1(TextWriter writer)
        {
            writer.WriteLine("# timecode format v1");
            var modeNumber = _intervalList.GroupBy(i => i.Interval)
                .OrderByDescending(g => g.Count())
                .First().Key;
            writer.WriteLine($"Assume {1e7 / modeNumber:F6}");

            foreach (var interval in _intervalList.Where(interval => interval.Interval != modeNumber))
            {
                writer.WriteLine($"{interval.StartFrame},{interval.EndFrame},{1e7 / interval.Interval:F6}");
            }
        }

        private void SaveTimecodeV2(TextWriter writer)
        {
            var frame = 0;
            double time = 0;
            writer.WriteLine("# timecode format v2");
            foreach (var interval in _intervalList)
            {
                while (frame <= interval.EndFrame)
                {
                    writer.WriteLine((time / 1e4).ToString("F6"));
                    time += interval.Interval;
                    ++frame;
                }
            }
        }
    }
}