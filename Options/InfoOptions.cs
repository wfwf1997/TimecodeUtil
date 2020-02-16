using CommandLine;

namespace TimecodeUtils.Options
{
    [Verb("info", HelpText = "Show basic info of a timecode file")]
    class InfoOptions
    {
        [Value(0, MetaName = "Input File", HelpText = "Timecode file path", Required = true)]
        public string Path { get; set; } = string.Empty;

        [Value(1, MetaName = "Total frames", HelpText = "Total frames of the file")]
        public int Length { get; set; } = 0;
    }
}