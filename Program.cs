using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TimecodeUtils.Options;
using TimecodeUtils.Timecode;

namespace TimecodeUtils
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintHelp(Console.Out);
                return;
            }

            Timecode.Timecode timecode;
            if (args[0] == "-")
            {
                timecode = new Timecode.Timecode(Console.In);
            }
            else
            {
                var inputInfo = new FileInfo(args[0]);
                if (!inputInfo.Exists)
                {
                    PrintErrorAndHelp($"Error: File not found! {inputInfo.FullName}");
                    return;
                }

                timecode = new Timecode.Timecode(inputInfo.FullName);
            }

            try
            {
                var restOpts = args.Skip(2).ToArray();
                switch (args[1])
                {
                    case "info":
                        InfoActionHandler(timecode, restOpts);
                        return;
                    case "convert":
                        ConvertActionHandler(timecode, restOpts);
                        return;
                    default:
                        PrintErrorAndHelp($"Error: No such action! {args[1]}");
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected Error:");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        private static void InfoActionHandler(Timecode.Timecode timecode, string[] opts)
        {
            switch (opts.Length)
            {
                case 0:
                    break;
                case 1 when timecode.Version == TimecodeVersion.V1 && int.TryParse(opts[0], out var length):
                    timecode.TotalFrames = length;
                    break;
                default:
                    PrintErrorAndHelp("Error: Too many arguments!");
                    return;
            }

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

        private static void ConvertActionHandler(Timecode.Timecode timecode, string[] opts)
        {
            if (opts.Length == 0)
            {
                PrintErrorAndHelp("Error: Too few arguments!");
                return;
            }

            var options = new ConvertOptions
            {
                Output = opts[0],
                Version = GetInvertTimecodeVersion(timecode.Version)
            };

            if (opts.Length > 1)
            {
                if (opts[1] == "--fix" || opts[1] == "-f")
                {
                    options.Version = timecode.Version;
                    switch (opts.Length)
                    {
                        case 2:
                            break;
                        case 3 when options.Version == TimecodeVersion.V1 && TryParseFrameRate(opts[2], out var fps):
                            options.Fps = fps;
                            break;
                        case 3 when options.Version == TimecodeVersion.V1:
                            PrintErrorAndHelp("Error: Can not parsing arguments");
                            return;
                        case 4 when options.Version == TimecodeVersion.V1 && TryParseFrameRate(opts[2], out var fps) &&
                                    int.TryParse(opts[3], out var length):
                            timecode.TotalFrames = length;
                            options.Fps = fps;
                            break;
                        case 4 when options.Version == TimecodeVersion.V1:
                            PrintErrorAndHelp("Error: Can not parsing arguments");
                            return;
                        default:
                            PrintErrorAndHelp("Error: Too many arguments!");
                            return;
                    }
                }
                else
                {
                    if (opts.Length > 3)
                    {
                        PrintErrorAndHelp("Error: Too many arguments!");
                        return;
                    }

                    switch (options.Version)
                    {
                        case TimecodeVersion.V1 when TryParseFrameRate(opts[1], out var fps):
                            options.Fps = fps;
                            break;
                        case TimecodeVersion.V2 when int.TryParse(opts[1], out var length):
                            timecode.TotalFrames = length;
                            break;
                        default:
                            PrintErrorAndHelp("Error: Can not parsing arguments");
                            return;
                    }
                }
            }

            if (options.Output == "-")
            {
                timecode.SaveTimecode(Console.Out, options.Version, options.Fps ?? 0);
            }
            else
            {
                timecode.SaveTimecode(options.Output, options.Version, options.Fps ?? 0);
            }
        }

        private static bool TryParseFrameRate(string str, out double fps)
        {
            fps = default;
            if (double.TryParse(str, out fps)) return true;
            var fractionReg = new Regex(@"(\d+)/(\d+)");
            var match = fractionReg.Match(str);
            if (!match.Success) return false;

            var num = int.Parse(match.Groups[1].Value);
            var den = int.Parse(match.Groups[2].Value);
            if (num == 0) return false;
            fps = 1.0 * num / den;

            return true;
        }

        private static TimecodeVersion GetInvertTimecodeVersion(TimecodeVersion version)
        {
            switch (version)
            {
                case TimecodeVersion.V1:
                    return TimecodeVersion.V2;
                case TimecodeVersion.V2:
                    return TimecodeVersion.V1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, null);
            }
        }

        private static void PrintErrorAndHelp(string error)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
            PrintHelp();
        }

        private static void PrintHelp()
        {
            PrintHelp(Console.Error);
        }

        private static void PrintHelp(TextWriter textWriter)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            textWriter.WriteLine($"Timecode Utils v{versionInfo.FileVersion}");
            textWriter.WriteLine(versionInfo.LegalCopyright);
            textWriter.WriteLine();
            textWriter.WriteLine("Usage:");
            textWriter.WriteLine("TimecodeUtils INPUT ACTION [...]");

            textWriter.WriteLine();
            textWriter.WriteLine("Action:");
            textWriter.WriteLine("\tinfo: Show information about a timecode file");
            textWriter.WriteLine("\t\tTimecodeUtils INPUT info [LENGTH]");
            textWriter.WriteLine("\tconvert: Convert a timecode file");
            textWriter.WriteLine("\t\tTimecodeUtils INPUT convert OUTPUT --fix [FPS(V1) [LENGTH(V1)]]");
            textWriter.WriteLine("\t\tTimecodeUtils INPUT convert OUTPUT [LENGTH(V1)|FPS(V2)]");
        }
    }
}
