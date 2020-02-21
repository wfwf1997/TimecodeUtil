using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

            var inputInfo = new FileInfo(args[0]);
            if (!inputInfo.Exists)
            {
                Console.Error.WriteLine($"Error: File not found! {inputInfo.FullName}");
                PrintHelp();
                return;
            }

            try
            {
                var restOpts = args.Skip(2).ToArray();
                switch (args[1])
                {
                    case "info":
                        InfoActionHandler(inputInfo, restOpts);
                        return;
                    case "convert":
                        ConvertActionHandler(inputInfo, restOpts);
                        return;
                    default:
                        PrintHelp();
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected Error:");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                Console.Error.WriteLine();
                PrintHelp();
            }
        }

        private static void InfoActionHandler(FileInfo fileInfo, string[] opts)
        {
            Timecode.Timecode timecode;
            switch (opts.Length)
            {
                case 0:
                    timecode = new Timecode.Timecode(fileInfo.FullName);
                    break;
                case 1 when int.TryParse(opts[0], out var length):
                    timecode = new Timecode.Timecode(fileInfo.FullName, length);
                    break;
                default:
                    PrintHelp();
                    return;
            }

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

        private static void ConvertActionHandler(FileInfo fileInfo, string[] opts)
        {
            var timecode = new Timecode.Timecode(fileInfo.FullName);
            var options = new ConvertOptions();
            switch (opts.Length)
            {
                case 0:
                {
                    options.OutputVersion = GetInvertTimecodeVersion(timecode.Meta.Version);
                    options.Output = Path.ChangeExtension(fileInfo.FullName,
                        $".{options.OutputVersion.ToString().ToLower()}{fileInfo.Extension}");
                    break;
                }
                case 1 when TryParseVersion(opts[0], out var version):
                    options.OutputVersion = version;
                    options.Output = Path.ChangeExtension(fileInfo.FullName,
                        $".{options.OutputVersion.ToString().ToLower()}{fileInfo.Extension}");
                    break;
                case 1 when int.TryParse(opts[0], out var length):
                    timecode.SetTotalFrames(length);
                    options.OutputVersion = GetInvertTimecodeVersion(timecode.Meta.Version);
                    options.Output = Path.ChangeExtension(fileInfo.FullName,
                        $".{options.OutputVersion.ToString().ToLower()}{fileInfo.Extension}");
                    break;
                case 1:
                    options.OutputVersion = GetInvertTimecodeVersion(timecode.Meta.Version);
                    options.Output = opts[0];
                    break;
                case 2 when TryParseVersion(opts[0], out var version) && int.TryParse(opts[1], out var length):
                    timecode.SetTotalFrames(length);
                    options.OutputVersion = version;
                    options.Output = Path.ChangeExtension(fileInfo.FullName,
                        $".{options.OutputVersion.ToString().ToLower()}{fileInfo.Extension}");
                    break;
                case 2 when int.TryParse(opts[0], out var length) && TryParseVersion(opts[1], out var version):
                    timecode.SetTotalFrames(length);
                    options.OutputVersion = version;
                    options.Output = Path.ChangeExtension(fileInfo.FullName,
                        $".{options.OutputVersion.ToString().ToLower()}{fileInfo.Extension}");
                    break;
                case 2 when TryParseVersion(opts[1], out var version):
                    options.OutputVersion = version;
                    options.Output = opts[0];
                    break;
                case 2 when int.TryParse(opts[1], out var length):
                    timecode.SetTotalFrames(length);
                    options.OutputVersion = GetInvertTimecodeVersion(timecode.Meta.Version);
                    options.Output = opts[0];
                    break;
                case 3 when TryParseVersion(opts[1], out var version) && int.TryParse(opts[2], out var length):
                    timecode.SetTotalFrames(length);
                    options.OutputVersion = version;
                    options.Output = opts[0];
                    break;
                case 3 when int.TryParse(opts[1], out var length) && TryParseVersion(opts[2], out var version):
                    timecode.SetTotalFrames(length);
                    options.OutputVersion = version;
                    options.Output = opts[0];
                    break;
                default:
                    PrintHelp();
                    return;
            }

            if (options.Output == "-")
            {
                timecode.SaveTimecode(Console.Out, options.OutputVersion);
            }
            else
            {
                timecode.SaveTimecode(options.Output, options.OutputVersion);
            }
        }

        private static bool TryParseVersion(string str, out TimecodeVersion version)
        {
            var verReg = new Regex("v(1|2)");
            var match = verReg.Match(str);
            if (!match.Success)
            {
                version = default;
                return false;
            }

            switch (match.Captures[1].Value)
            {
                case "1":
                    version = TimecodeVersion.V1;
                    break;
                case "2":
                    version = TimecodeVersion.V2;
                    break;
                default:
                    version = default;
                    return false;
            }

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
            textWriter.WriteLine("TimecodeUtils INPUT METHOD [...]");

            textWriter.WriteLine();
            textWriter.WriteLine("Method:");
            textWriter.WriteLine("\tinfo: Show information about a timecode file");
            textWriter.WriteLine("\t\tTimecodeUtils INPUT info [LENGTH]");
            textWriter.WriteLine("\tconvert: Convert a timecode file");
            textWriter.WriteLine("\t\tTimecodeUtils INPUT convert [OUTPUT] [LENGTH|VERSION] [LENGTH|VERSION]");
        }
    }
}
