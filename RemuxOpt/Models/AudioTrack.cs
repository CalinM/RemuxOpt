namespace RemuxOpt
{
    public class AudioTrack
    {
        public string Language { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Channels { get; set; }
        public int? BitRate { get; set; }  // from ffprobe
    }
}
