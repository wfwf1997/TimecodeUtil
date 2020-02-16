using CommandLine;

namespace TimecodeUtils.Options
{
    [Verb("convert", HelpText = "Convert between timecode v1 and v2")]
    class ConvertOptions
    {
        [Value(0, MetaName = "Input File", HelpText = "Timecode file path", Required = true)]
        public string Path { get; set; } = string.Empty;

        [Option('l', "length", HelpText = "Total frames of the file")]
        public int Length { get; set; } = 0;

        [Option('f', "fix", HelpText = "Fix timecode only")]
        public bool Fix { get; set; } = false;

        [Option('o', "output", HelpText = "Path for output")]
        public string Output { get; set; } = string.Empty;
    }
}