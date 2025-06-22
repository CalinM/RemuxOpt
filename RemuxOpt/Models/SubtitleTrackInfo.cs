namespace RemuxOpt
{
    public class SubtitleTrackInfo
    {
        public int FileId { get; set; }
        public int TrackId { get; set; }
        public string Language { get; set; } = "";
        public bool IsForced { get; set; }
        public string FileName { get; set; } = "";
        public string Title { get; set; } = string.Empty;
    }
}
