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
        public List<ExternalAudioTrack> ExternalAudioFiles { get; set; } = [];
        public List<SubtitleTrackInfo> Subtitles { get; set; } = [];
        public List<Attachment> Attachments { get; set; } = [];
    }
}
