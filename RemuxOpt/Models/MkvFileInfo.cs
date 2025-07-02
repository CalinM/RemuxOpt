namespace RemuxOpt
{
    public class MkvFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath
        {
            get
            {
                return Path.GetDirectoryName(FileName);
            }
        }
        public List<AudioTrackInfo> AudioTracks { get; set; } = [];
        public List<AudioTrackInfo> ExternalAudioTracks { get; set; } = [];
        public List<SubtitleTrackInfo> SubtitleTracks { get; set; } = [];
        public List<Attachment> Attachments { get; set; } = [];
    }
}
