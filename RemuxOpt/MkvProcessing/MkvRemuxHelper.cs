﻿using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RemuxOpt
{
    public class MkvRemuxHelper
    {
        public bool UseAutoTitle { get; set; }
        public bool RemoveAttachments { get; set; }
        public bool RemoveForcedFlags { get; set; }
        public bool RemoveFileTitle { get; set; }
        public string DefaultTrackLanguageCode { get; set; } = "";
        public string OutputFolder { get; set; } = "";
        public List<string> AudioLanguageOrder { get; set; } = new();
        public List<string> SubtitleLanguageOrder { get; set; } = new();

        public string MkvMergePath { get; set; } = "mkvmerge";
        public string FfprobePath { get; set; } = "ffprobe";

        public string RunMkvMergeJson(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = MkvMergePath,
                Arguments = $"-J \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo)!;
            using var reader = process.StandardOutput;
            string output = reader.ReadToEnd();
            process.WaitForExit();

            return output;
        }

        public string RunFfprobeJson(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = FfprobePath,
                Arguments = $"-v quiet -print_format json -show_streams -select_streams a \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo)!;
            using var reader = process.StandardOutput;
            string output = reader.ReadToEnd();
            process.WaitForExit();

            return output;
        }

        public AudioTrackInfo? GetAudioInfo(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "mediainfo",
                Arguments = $"--Output=JSON \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(output)) 
                return null;

            using JsonDocument doc = JsonDocument.Parse(output);

            var audioTrack = doc.RootElement
                .GetProperty("media")
                .GetProperty("track")
                .EnumerateArray()
                .FirstOrDefault(t => t.GetProperty("@type").GetString() == "Audio");

            if (audioTrack.ValueKind == JsonValueKind.Undefined)
                return null;

            int bitRate = 0;
            int channels = 0;

            if (audioTrack.TryGetProperty("BitRate", out var brProp))
            {
                if (brProp.ValueKind == JsonValueKind.String && int.TryParse(brProp.GetString(), out int br))
                    bitRate = br;
                else if (brProp.ValueKind == JsonValueKind.Number)
                    bitRate = brProp.GetInt32();
            }

            if (audioTrack.TryGetProperty("Channels", out var chProp))
            {
                if (chProp.ValueKind == JsonValueKind.String && int.TryParse(chProp.GetString(), out int ch))
                    channels = ch;
                else if (chProp.ValueKind == JsonValueKind.Number)
                    channels = chProp.GetInt32();
            }

            return new AudioTrackInfo
            {
                BitRate = bitRate,
                Channels = channels
            };
        }

        public (string arguments, string outputFilePath) BuildMkvMergeArgs(MkvFileInfo fileInfo)
        {
            var audioTracks = new List<AudioTrackInfo>();
            var subtitleTracks = new List<SubtitleTrackInfo>();
            
            string mkvJson = RunMkvMergeJson(fileInfo.FileName);
            using var mkvDoc = JsonDocument.Parse(mkvJson);
            var tracks = mkvDoc.RootElement.GetProperty("tracks");

            string ffJson = RunFfprobeJson(fileInfo.FileName);
            using var ffDoc = JsonDocument.Parse(ffJson);
            var bitrateMap = new Dictionary<int, int>();

            foreach (var stream in ffDoc.RootElement.GetProperty("streams").EnumerateArray())
            {
                if (stream.GetProperty("codec_type").GetString() != "audio")
                {
                    continue;
                }

                var idx = stream.GetProperty("index").GetInt32();
                
                if (stream.TryGetProperty("bit_rate", out var brProp))
                {
                    if (brProp.ValueKind == JsonValueKind.Number && brProp.TryGetInt32(out int br))
                    { 
                        bitrateMap[idx] = br;
                    }
                    else if (brProp.ValueKind == JsonValueKind.String && int.TryParse(brProp.GetString(), out br))
                    { 
                        bitrateMap[idx] = br;
                    }
                }
            }

            // Process audio tracks
            foreach (var t in tracks.EnumerateArray().Where(t => t.GetProperty("type").GetString() == "audio"))
            {
                int id = t.GetProperty("id").GetInt32();
                var props = t.GetProperty("properties");
                string lang = props.TryGetProperty("language", out var lp) ? lp.GetString() ?? "und" : "und";
                string codecId = props.TryGetProperty("codec_id", out var cd) ? cd.GetString() ?? "" : "";
                int channels = props.TryGetProperty("audio_channels", out var ch) && ch.TryGetInt32(out var c) ? c : 0;
                int bitRate = bitrateMap.TryGetValue(id, out var br2) ? br2 : 0;
                bool isForced = props.TryGetProperty("forced_track", out var forced) &&
                              (forced.ValueKind == JsonValueKind.True || 
                               (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1));

                audioTracks.Add(new AudioTrackInfo
                {
                    FileId = 0,
                    TrackId = id,
                    Language = lang,
                    CodecId = codecId,
                    Channels = channels,
                    BitRate = bitRate,
                    IsForced = isForced,
                    FileName = fileInfo.FileName
                });
            }

            // Process subtitle tracks
            foreach (var t in tracks.EnumerateArray().Where(t => t.GetProperty("type").GetString() == "subtitles"))
            {
                int id = t.GetProperty("id").GetInt32();
                var props = t.GetProperty("properties");
                string lang = props.TryGetProperty("language", out var lp) ? lp.GetString() ?? "und" : "und";
                bool isForced = props.TryGetProperty("forced_track", out var forced) &&
                              (forced.ValueKind == JsonValueKind.True || 
                               (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1));

                subtitleTracks.Add(new SubtitleTrackInfo
                {
                    FileId = 0,
                    TrackId = id,
                    Language = lang,
                    IsForced = isForced,
                    FileName = fileInfo.FileName
                });
            }

            int externalFileIndex = 1;
            foreach (var externalAudioFile in fileInfo.ExternalAudioFiles)
            {
                string ext = Path.GetExtension(externalAudioFile.FileName).ToLowerInvariant();
                string lang = !string.IsNullOrEmpty(externalAudioFile.LanguageCode) ? externalAudioFile.LanguageCode : "und";

                if (ext == ".mka")
                {
                    string extJson = RunMkvMergeJson(externalAudioFile.FileName);
                    using var extDoc = JsonDocument.Parse(extJson);
                    var extTracks = extDoc.RootElement.GetProperty("tracks")
                        .EnumerateArray()
                        .Where(t => t.GetProperty("type").GetString() == "audio");

                    var mediaInfo = GetAudioInfo(externalAudioFile.FileName);

                    foreach (var extTrack in extTracks)
                    {
                        int trackId = extTrack.GetProperty("id").GetInt32();
                        var props = extTrack.GetProperty("properties");
                        string codecId = props.TryGetProperty("codec_id", out var cd) ? cd.GetString() ?? "" : "";
                        int channels = props.TryGetProperty("audio_channels", out var ch) && ch.TryGetInt32(out var c) ? c : 0;
                        int bitRate = mediaInfo?.BitRate ?? 0;

                        audioTracks.Add(new AudioTrackInfo
                        {
                            FileId = externalFileIndex,
                            TrackId = trackId,
                            Language = lang,
                            CodecId = codecId,
                            Channels = channels,
                            BitRate = bitRate,
                            IsForced = false, // External audio files typically don't have forced flags
                            FileName = externalAudioFile.FileName
                        });
                    }
                }
                else
                {
                    var mediaInfo = GetAudioInfo(externalAudioFile.FileName);
                    audioTracks.Add(new AudioTrackInfo
                    {
                        FileId = externalFileIndex,
                        TrackId = 0,
                        Language = lang,
                        Channels = mediaInfo?.Channels ?? 0,
                        BitRate = mediaInfo?.BitRate ?? 0,
                        IsForced = false, // External audio files typically don't have forced flags
                        FileName = externalAudioFile.FileName
                    });
                }
                externalFileIndex++;
            }

            // Sort audio and subtitle tracks by language preference
            int AudioLangPriority(string lang) => AudioLanguageOrder.IndexOf(lang) is var i && i >= 0 ? i : int.MaxValue;
            int SubtitleLangPriority(string lang) => SubtitleLanguageOrder.IndexOf(lang) is var i && i >= 0 ? i : int.MaxValue;
            
            var sortedAudio = audioTracks.OrderBy(t => AudioLangPriority(t.Language)).ThenBy(t => t.FileId).ThenBy(t => t.TrackId).ToList();
            var sortedSubtitles = subtitleTracks.OrderBy(t => SubtitleLangPriority(t.Language)).ThenBy(t => t.TrackId).ToList();

            List<string> args = [];
            List<string> trackOrder = [];

            // Global options first
            var baseDir = Path.GetDirectoryName(fileInfo.FileName);
            var fileName = Path.GetFileName(fileInfo.FileName);

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

            // Determine which track should be default
            AudioTrackInfo defaultTrack = null;
            if (!string.IsNullOrWhiteSpace(DefaultTrackLanguageCode))
            {
                // Find first track matching the DefaultTrack language
                defaultTrack = sortedAudio.FirstOrDefault(t => t.Language.Equals(DefaultTrackLanguageCode, StringComparison.OrdinalIgnoreCase));
            }
            // Fallback to first track in sorted order if DefaultTrack not found or not specified
            defaultTrack ??= sortedAudio.FirstOrDefault();

            // Process main MKV file
            var mainFileAudioTracks = sortedAudio.Where(t => t.FileId == 0).ToList();
            var mainFileSubtitleTracks = sortedSubtitles.Where(t => t.FileId == 0).ToList();
            var allMainTracks = tracks.EnumerateArray().ToList();
    
            // Determine which audio tracks to keep from main file
            var audioTracksToKeep = mainFileAudioTracks.Select(t => t.TrackId).ToList();
            var allAudioTrackIds = allMainTracks
                .Where(t => t.GetProperty("type").GetString() == "audio")
                .Select(t => t.GetProperty("id").GetInt32())
                .ToList();

            // Determine which subtitle tracks to keep from main file
            var subtitleTracksToKeep = mainFileSubtitleTracks.Select(t => t.TrackId).ToList();
            var allSubtitleTrackIds = allMainTracks
                .Where(t => t.GetProperty("type").GetString() == "subtitles")
                .Select(t => t.GetProperty("id").GetInt32())
                .ToList();

            // Only include audio tracks we want
            if (audioTracksToKeep.Count < allAudioTrackIds.Count)
                args.Add($"-a {string.Join(',', audioTracksToKeep)}");

            // Only include subtitle tracks we want
            if (subtitleTracksToKeep.Count > 0 && subtitleTracksToKeep.Count < allSubtitleTrackIds.Count)
                args.Add($"-s {string.Join(',', subtitleTracksToKeep)}");
            else if (subtitleTracksToKeep.Count == 0)
                args.Add("-S"); // No subtitles

            // Add track-specific options for main file audio tracks
            foreach (var track in mainFileAudioTracks)
            {
                bool isDefault = defaultTrack != null && track.FileId == defaultTrack.FileId && track.TrackId == defaultTrack.TrackId;
        
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
                args.Add($"--language {track.TrackId}:{track.Language}");
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
            foreach (var externalAudioFile in fileInfo.ExternalAudioFiles)
            {
                var externalTracks = sortedAudio.Where(t => t.FileId == currentFileIndex).ToList();
        
                // Add track-specific options for this external file
                foreach (var track in externalTracks)
                {
                    bool isDefault = defaultTrack != null && track.FileId == defaultTrack.FileId && track.TrackId == defaultTrack.TrackId;
            
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

            //return new KeyValuePair<string, string>(string.Join(' ', args), outputFile);
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