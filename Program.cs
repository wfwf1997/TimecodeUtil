using System;
using System.IO;
using CommandLine;
using TimecodeUtils.Options;
using TimecodeUtils.Timecode;

namespace TimecodeUtils
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<InfoOptions, ConvertOptions>(args)
                .WithParsed<InfoOptions>(InfoActionHandler)
                .WithParsed<ConvertOptions>(ConvertActionHandler)
                .WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.Tag);
                    }
                });
        }

        private static void InfoActionHandler(InfoOptions opts)
        {
            var fileInfo = new FileInfo(opts.Path);
            if (!fileInfo.Exists)
            {
                Console.Error.WriteLine($"Error: File not found! {fileInfo.FullName}");
                return;
            }

            var timecode = new Timecode.Timecode(opts.Path, opts.Length);
            Console.WriteLine($" Timecode File: {fileInfo.FullName}");
            Console.WriteLine(
                $" {"Total Length: ",-20}{timecode.TotalLength,-15:hh\\:mm\\:ss\\.fff}{"Total Frames: ",-20}{timecode.TotalFrames}");
            Console.WriteLine(
                $" {"Average Frame Rate: ",-20}{timecode.AverageFrameRate,-15:F3}{"Default Frame Rate: ",-20}{timecode.DefaultFrameRate:F3}");

            Console.WriteLine($"+{new string('-', 23)}+{new string('-', 23)}+{new string('-', 12)}+");
            Console.WriteLine($"| {"Start Frame / Time",21} | {"End Frame / Time",21} | {"Frame Rate",10} |");
            Console.WriteLine($"+{new string('-', 23)}+{new string('-', 23)}+{new string('-', 12)}+");
            foreach (var interval in timecode.IntervalList)
            {
                Console.WriteLine(
                    $"| {interval.StartFrame,6} / {timecode.GetTimeSpanFromFrameNumber(interval.StartFrame),-12:hh\\:mm\\:ss\\.fff} " +
                    $"| {interval.EndFrame,6} / {timecode.GetTimeSpanFromFrameNumber(interval.EndFrame),-12:hh\\:mm\\:ss\\.fff} " +
                    $"| {1e7 / interval.Interval,10:F6} |");
            }

            Console.WriteLine($"+{new string('-', 23)}+{new string('-', 23)}+{new string('-', 12)}+");
        }

        private static void ConvertActionHandler(ConvertOptions opts)
        {
            var fileInfo = new FileInfo(opts.Path);
            if (!fileInfo.Exists)
            {
                Console.Error.WriteLine($"Error: File not found! {fileInfo.FullName}");
                return;
            }

            var timecode = new Timecode.Timecode(opts.Path, opts.Length);
            if (timecode.Version == TimecodeVersion.V1 && opts.Fix
                || timecode.Version == TimecodeVersion.V2 && !opts.Fix)
            {
                if (opts.Output == "-")
                {
                    timecode.SaveTimecode(Console.OpenStandardOutput(), TimecodeVersion.V1);
                }
                else
                {
                    timecode.SaveTimecode(
                        string.IsNullOrWhiteSpace(opts.Output)
                            ? Path.ChangeExtension(fileInfo.FullName, ".v1" + fileInfo.Extension)
                            : opts.Output, TimecodeVersion.V1);
                }
            }
            else
            {
                if (opts.Output == "-")
                {
                    timecode.SaveTimecode(Console.OpenStandardOutput());
                }
                else
                {
                    timecode.SaveTimecode(
                        string.IsNullOrWhiteSpace(opts.Output)
                            ? Path.ChangeExtension(fileInfo.FullName, ".v2" + fileInfo.Extension)
                            : opts.Output);
                }
            }
        }
    }
}