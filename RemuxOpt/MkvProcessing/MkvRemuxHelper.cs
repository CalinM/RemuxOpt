namespace RemuxOpt
{
    public class MkvRemuxHelper
    {
        public bool UseAutoTitle { get; set; }
        public bool RemoveAttachments { get; set; }
        public bool RemoveForcedFlags { get; set; }
        public bool RemoveFileTitle { get; set; }
        public bool RemoveUnlistedLanguageTracks { get; set; }
        public string DefaultAudioTrackLanguageCode { get; set; } = "";
        public string DefaultSubtitleTrackLanguageCode { get; set; } = "";
        public string OutputFolder { get; set; } = "";
        public List<string> AudioLanguageOrder { get; set; } = [];
        public List<string> SubtitleLanguageOrder { get; set; } = [];

        public (string arguments, string outputFilePath) BuildMkvMergeArgs(MkvFileInfo fileInfo)
        {
            fileInfo.AudioTracks.AddRange(fileInfo.ExternalAudioTracks);

            // Filter out tracks not in language order lists if option is enabled
            var audioTracksToProcess = fileInfo.AudioTracks;
            var subtitleTracksToProcess = fileInfo.SubtitleTracks;

            if (RemoveUnlistedLanguageTracks)
            {
                // Only keep audio tracks that are in the AudioLanguageOrder list
                if (AudioLanguageOrder.Count > 0)
                {
                    audioTracksToProcess = audioTracksToProcess
                        .Where(t => AudioLanguageOrder.Any(lang => lang.Equals(t.Language, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Only keep subtitle tracks that are in the SubtitleLanguageOrder list
                if (SubtitleLanguageOrder.Count > 0)
                {
                    subtitleTracksToProcess = subtitleTracksToProcess
                        .Where(t => SubtitleLanguageOrder.Any(lang => lang.Equals(t.Language, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            }

            // Sort audio and subtitle tracks by language preference
            int AudioLangPriority(string lang) => AudioLanguageOrder.IndexOf(lang) is var i && i >= 0 ? i : int.MaxValue;
            int SubtitleLangPriority(string lang) => SubtitleLanguageOrder.IndexOf(lang) is var i && i >= 0 ? i : int.MaxValue;

            var sortedAudio = audioTracksToProcess.OrderBy(t => AudioLangPriority(t.Language)).ThenBy(t => t.FileId).ThenBy(t => t.TrackId).ToList();
            var sortedSubtitles = subtitleTracksToProcess.OrderBy(t => SubtitleLangPriority(t.Language)).ThenBy(t => t.TrackId).ToList();

            List<string> args = [];
            List<string> trackOrder = [];

            // Global options first
            var baseDir = Path.GetDirectoryName(fileInfo.FileName);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.FileName) + ".mkv";

            var safeOutputFolder = OutputFolder.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var outputFile = OutputFolder == "\\Output"
                ? Path.Combine(baseDir, safeOutputFolder, fileName)
                : Path.Combine(OutputFolder, fileName);

            args.Add($"-o \"{outputFile}\"");

            if (RemoveFileTitle)
            {
                args.Add("--title \"\"");
                args.Add("--no-global-tags");
            }

            if (RemoveAttachments)
                args.Add("--no-attachments");

            // Build track order based on sorted audio and subtitles
            for (int i = 0; i < sortedAudio.Count; i++)
            {
                var track = sortedAudio[i];
                trackOrder.Add($"{track.FileId}:{track.TrackId}");
            }

            for (int i = 0; i < sortedSubtitles.Count; i++)
            {
                var track = sortedSubtitles[i];
                trackOrder.Add($"{track.FileId}:{track.TrackId}");
            }

            // Determine which audio track should be default
            AudioTrackInfo defaultAudioTrack = null;
            if (!string.IsNullOrWhiteSpace(DefaultAudioTrackLanguageCode))
            {
                // Find first track matching the DefaultAudioTrack language
                defaultAudioTrack = sortedAudio.FirstOrDefault(t => t.Language.Equals(DefaultAudioTrackLanguageCode, StringComparison.OrdinalIgnoreCase));
            }
            else if (AudioLanguageOrder.Count > 0)
            {
                // Use first language in AudioLanguageOrder as default
                var firstLanguage = AudioLanguageOrder[0];
                defaultAudioTrack = sortedAudio.FirstOrDefault(t => t.Language.Equals(firstLanguage, StringComparison.OrdinalIgnoreCase));
            }
            // Fallback to first track in sorted order if nothing found
            defaultAudioTrack ??= sortedAudio.FirstOrDefault();

            // Determine which subtitle track should be default
            SubtitleTrackInfo defaultSubtitleTrack = null;
            if (!string.IsNullOrWhiteSpace(DefaultSubtitleTrackLanguageCode))
            {
                // Find first track matching the DefaultSubtitleTrack language
                defaultSubtitleTrack = sortedSubtitles.FirstOrDefault(t => t.Language.Equals(DefaultSubtitleTrackLanguageCode, StringComparison.OrdinalIgnoreCase));
            }
            // Note: If DefaultSubtitleTrackLanguageCode is not specified, no subtitle track will be default

            // Process main MKV file
            var mainFileAudioTracks = sortedAudio.Where(t => t.FileId == 0).ToList();
            var mainFileSubtitleTracks = sortedSubtitles.Where(t => t.FileId == 0).ToList();

            // Determine which audio tracks to keep from main file
            var audioTracksToKeep = mainFileAudioTracks.Select(t => t.TrackId).ToList();
            var allAudioTrackIds = fileInfo.AudioTracks.Where(t => t.FileId == 0).Select(t => t.TrackId).ToList();

            // Determine which subtitle tracks to keep from main file
            var subtitleTracksToKeep = mainFileSubtitleTracks.Select(t => t.TrackId).ToList();
            var allSubtitleTrackIds = fileInfo.SubtitleTracks.Where(t => t.FileId == 0).Select(t => t.TrackId).ToList();

            // Only include audio tracks we want
            if (audioTracksToKeep.Count < allAudioTrackIds.Count)
            {
                args.Add($"-a {string.Join(',', audioTracksToKeep)}");
            }

            // Always specify which subtitle tracks to include (even if it's all of them)
            if (subtitleTracksToKeep.Count > 0)
            {
                args.Add($"-s {string.Join(',', subtitleTracksToKeep)}");
            }
            else
            {
                args.Add("-S"); // No subtitles
            }

            // Add track-specific options for main file audio tracks
            foreach (var track in mainFileAudioTracks)
            {
                bool isDefault = defaultAudioTrack != null && track.FileId == defaultAudioTrack.FileId && track.TrackId == defaultAudioTrack.TrackId;

                args.Add($"--language {track.TrackId}:{track.Language}");
                args.Add($"--default-track-flag {track.TrackId}:{(isDefault ? "yes" : "no")}");

                if (RemoveForcedFlags && track.IsForced)
                {
                    args.Add($"--forced-track {track.TrackId}:no");
                }

                string title = "";
                if (UseAutoTitle)
                {
                    string langName = GetLanguageName(track.Language);
                    string audioType = !string.IsNullOrEmpty(track.CodecId) ? GetAudioType(track.CodecId) : GetAudioTypeFromBitrateAndExtension(track.BitRate, Path.GetExtension(track.FileName));
                    string channelDesc = FormatChannels(track.Channels);
                    string bitrateDesc = track.BitRate > 0 ? $"{track.BitRate / 1000} kbps" : "unknown bitrate";
                    title = $"{langName} {audioType} {channelDesc} @ {bitrateDesc}";
                }
                args.Add($"--track-name {track.TrackId}:\"{title}\"");
            }

            // Add track-specific options for main file subtitle tracks
            foreach (var track in mainFileSubtitleTracks)
            {
                bool isDefault = defaultSubtitleTrack != null && track.FileId == defaultSubtitleTrack.FileId && track.TrackId == defaultSubtitleTrack.TrackId;

                args.Add($"--language {track.TrackId}:{track.Language}");
                args.Add($"--default-track-flag {track.TrackId}:{(isDefault ? "yes" : "no")}");
                args.Add($"--sub-charset {track.TrackId}:UTF-8");

                if (RemoveForcedFlags && track.IsForced)
                {
                    args.Add($"--forced-track {track.TrackId}:no");
                }
            }

            // Add main file
            args.Add($"\"{fileInfo.FileName}\"");

            // Process external files
            int currentFileIndex = 1;
            foreach (var externalAudioFile in fileInfo.ExternalAudioTracks)
            {
                var externalTracks = sortedAudio.Where(t => t.FileId == currentFileIndex).ToList();

                // Add track-specific options for this external file
                foreach (var track in externalTracks)
                {
                    bool isDefault = defaultAudioTrack != null && track.FileId == defaultAudioTrack.FileId && track.TrackId == defaultAudioTrack.TrackId;

                    args.Add($"--language {track.TrackId}:{track.Language}");
                    args.Add($"--default-track-flag {track.TrackId}:{(isDefault ? "yes" : "no")}");

                    if (RemoveForcedFlags && track.IsForced)
                    {
                        args.Add($"--forced-track {track.TrackId}:no");
                    }

                    string title = "";
                    if (UseAutoTitle)
                    {
                        string langName = GetLanguageName(track.Language);
                        string audioType = !string.IsNullOrEmpty(track.CodecId) ? GetAudioType(track.CodecId) : GetAudioTypeFromBitrateAndExtension(track.BitRate, Path.GetExtension(track.FileName));
                        string channelDesc = FormatChannels(track.Channels);
                        string bitrateDesc = track.BitRate > 0 ? $"{track.BitRate / 1000} kbps" : "unknown bitrate";
                        title = $"{langName} {audioType} {channelDesc} @ {bitrateDesc}";
                    }
                    args.Add($"--track-name {track.TrackId}:\"{title}\"");
                }

                // Add external file
                args.Add($"\"{externalAudioFile.FileName}\"");
                currentFileIndex++;
            }

            // Track order at the end - this is crucial for proper ordering
            if (trackOrder.Count > 0)
                args.Add($"--track-order {string.Join(',', trackOrder)}");

            return (string.Join(' ', args), outputFile);
        }

        private string GetLanguageName(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "Unknown";
            code = code.Trim().ToLowerInvariant();
            var match = Languages.Iso639.FirstOrDefault(lang =>
                lang.Abr2.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                lang.Abr3a.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                lang.Abr3b.Equals(code, StringComparison.OrdinalIgnoreCase));
            return match is null ? code.ToUpperInvariant() : match.Name.Replace("; Moldavian; Moldovan", string.Empty);
        }

        private string GetAudioType(string codecId) => codecId switch
        {
            "A_EAC3" => "DDP",
            "A_AC3" => "AC3",
            "A_AAC" => "AAC",
            "A_OPUS" => "Opus",
            "A_DTS" => "DTS",
            "A_TRUEHD" => "TrueHD",
            _ => "Audio"
        };

        private string GetAudioTypeFromBitrateAndExtension(int? bitRate, string extension) => extension switch
        {
            ".aac" => "AAC",
            ".ac3" => "AC3",
            ".eac3" => "E-AC3",
            ".dts" => "DTS",
            _ => bitRate == null ? string.Empty : bitRate >= 320000 ? "HQ Audio" : "Audio"
        };

        private string FormatChannels(int ch) => ch switch
        {
            1 => "1.0",
            2 => "2.0",
            6 => "5.1",
            8 => "7.1",
            _ => $"{ch}.0"
        };
    }
}