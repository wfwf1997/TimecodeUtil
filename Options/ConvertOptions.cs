using TimecodeUtils.Timecode;

namespace TimecodeUtils.Options
{
    class ConvertOptions
    {
        public TimecodeVersion OutputVersion { get; set; } = TimecodeVersion.V2;
        public string Output { get; set; } = string.Empty;
    }
}
