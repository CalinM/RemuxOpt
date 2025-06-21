namespace RemuxOpt
{
    public class MkvFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string OutputFileName
        {
            get
            {
                return Path.Combine(
                    Path.GetDirectoryName(FileName)!,
                    Path.GetFileNameWithoutExtension(FileName) + ".remuxed.mkv"
                );
            }
        }
        public List<AudioTrack> AudioTracks { get; set; } = [];
        public List<ExternalAudioTrack> ExternalAudioFiles { get; set; } = [];
        public List<SubtitleTrack> Subtitles { get; set; } = [];
        public List<Attachment> Attachments { get; set; } = [];
    }
}
