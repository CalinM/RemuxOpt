namespace RemuxOpt
{
    public class AudioTrackInfo
    {
        public int FileId { get; set; }        // 0 = main file, 1+ = external files
        public int TrackId { get; set; }       // track ID inside that file
        public string Language { get; set; }   // e.g. "eng", "dut"
        public string CodecId { get; set; }    // for title generation
        public int Channels { get; set; }
        public int? BitRate { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }   // needed to append file paths after arguments
    }
}
