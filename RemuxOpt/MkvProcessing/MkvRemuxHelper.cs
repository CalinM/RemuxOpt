using System.Diagnostics;
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

        public AudioTrack? GetAudioInfo(string filePath)
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
            { 
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(output);

            var audioTrack = doc.RootElement
                .GetProperty("media")
                .GetProperty("track")
                .EnumerateArray()
                .FirstOrDefault(t => t.GetProperty("@type").GetString() == "Audio");

            if (audioTrack.ValueKind == JsonValueKind.Undefined)
                return null;

            int bitrate = 0;
            int channels = 0;

            if (audioTrack.TryGetProperty("BitRate", out var brProp))
            {
                if (brProp.ValueKind == JsonValueKind.String && int.TryParse(brProp.GetString(), out int br))
                    bitrate = br;
                else if (brProp.ValueKind == JsonValueKind.Number)
                    bitrate = brProp.GetInt32();
            }

            if (audioTrack.TryGetProperty("Channels", out var chProp))
            {
                if (chProp.ValueKind == JsonValueKind.String && int.TryParse(chProp.GetString(), out int ch))
                    channels = ch;
                else if (chProp.ValueKind == JsonValueKind.Number)
                    channels = chProp.GetInt32();
            }

            return new AudioTrack
            {
                BitRate = bitrate,
                Channels = channels
            };
        }


        public string BuildMkvMergeArgs(MkvFileInfo fileInfo)
        {
            // mkvmerge JSON
            string mkvJson = RunMkvMergeJson(fileInfo.FileName);
            using var mkvDoc = JsonDocument.Parse(mkvJson);
            var tracks = mkvDoc.RootElement.GetProperty("tracks");

            // ffprobe JSON
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
                    var br = 0;
                    if (brProp.ValueKind == JsonValueKind.Number && brProp.TryGetInt32(out int num))
                    {
                        br = num;
                    }
                    else if (brProp.ValueKind == JsonValueKind.String && int.TryParse(brProp.GetString(), out int strNum))
                    {
                        br = strNum;
                    }

                    bitrateMap[idx] = br;
                }
            }

            var audioTracks = tracks.EnumerateArray()
                .Where(t => t.GetProperty("type").GetString() == "audio")
                .ToList();

            var subtitleTracks = tracks.EnumerateArray()
                .Where(t => t.GetProperty("type").GetString() == "subtitles")
                .ToList();

            List<string> args = [];
            List<string> trackOrder = [];
            int fileId = 0;

            var orderedAudio = new List<(int id, string lang)>();
            var orderedSubtitles = new List<(int id, string lang)>();

            // Order audio by preferred languages
            foreach (var lang in AudioLanguageOrder)
            {
                var matches = audioTracks.Where(t =>
                {
                    var props = t.GetProperty("properties");
                    string trackLang = props.TryGetProperty("language", out var lp) ? lp.GetString() ?? "und" : "und";
                    return trackLang == lang;
                });
                foreach (var t in matches)
                    orderedAudio.Add((t.GetProperty("id").GetInt32(), lang));
            }

            // Order subtitles by preferred languages
            foreach (var lang in SubtitleLanguageOrder)
            {
                var matches = subtitleTracks.Where(t =>
                {
                    var props = t.GetProperty("properties");
                    string trackLang = props.TryGetProperty("language", out var lp) ? lp.GetString() ?? "und" : "und";
                    return trackLang == lang;
                });
                foreach (var t in matches)
                    orderedSubtitles.Add((t.GetProperty("id").GetInt32(), lang));
            }

            // Audio tracks args
            if (AudioLanguageOrder.Count == 0)
            {
                args.Add("--no-audio");
            }
            else if (orderedAudio.Count > 0)
            {
                args.Add($"--audio-tracks {string.Join(',', orderedAudio.Select(x => x.id))}");
                for (int i = 0; i < orderedAudio.Count; i++)
                {
                    var (trackId, lang) = orderedAudio[i];
                    args.Add($"--language {trackId}:{lang}");

                    if (UseAutoTitle)
                    {
                        var track = audioTracks.First(t => t.GetProperty("id").GetInt32() == trackId);
                        var props = track.GetProperty("properties");

                        string langName = GetLanguageName(lang);
                        string codecId = props.TryGetProperty("codec_id", out var cd) ? cd.GetString() ?? string.Empty : string.Empty;
                        int channels = props.TryGetProperty("audio_channels", out var ch) && ch.TryGetInt32(out var c) ? c : 0;
                        int bitrate = bitrateMap.TryGetValue(trackId, out var fb) ? fb : 0;

                        string audioType = GetAudioType(codecId);
                        string channelDesc = FormatChannels(channels);
                        string bitrateDesc = bitrate > 0 ? $"{bitrate / 1000} kbps" : "unknown bitrate";

                        string title = $"{langName} {audioType} {channelDesc} @ {bitrateDesc}";
                        args.Add($"--track-name {trackId}:\"{title}\"");
                    }
                    else
                    {
                        args.Add($"--track-name {trackId}:\"\"");
                    }

                    args.Add($"--default-track-flag {trackId}:{(i == 0 ? "yes" : "no")} ");
                    trackOrder.Add($"{fileId}:{trackId}");
                }
            }

            // Add main file after all its options
            args.Add($"\"{fileInfo.FileName}\"");

            // External audio tracks args via helper
            int externalFileIndex = 1;
            foreach (var externalAudioFile in fileInfo.ExternalAudioFiles)
            {
                AddExternalAudioTrack(args, trackOrder, orderedAudio, externalAudioFile, externalFileIndex++);
            }

            // Subtitle tracks args
            if (SubtitleLanguageOrder.Count == 0)
            {
                args.Add("--no-subtitles");
            }
            else if (orderedSubtitles.Count > 0)
            {
                args.Add($"--subtitle-tracks {string.Join(',', orderedSubtitles.Select(x => x.id))}");
                foreach (var (trackId, lang) in orderedSubtitles)
                {
                    args.Add($"--language {trackId}:{lang}");
                    args.Add($"--sub-charset {trackId}:UTF-8");
                    if (RemoveForcedFlags)
                    {
                        var track = subtitleTracks.First(t => t.GetProperty("id").GetInt32() == trackId);
                        if (track.GetProperty("properties").TryGetProperty("forced_track", out var forced) &&
                            (forced.ValueKind == JsonValueKind.True || (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1)))
                        {
                            args.Add($"--forced-track {trackId}:no");
                        }
                    }
                    trackOrder.Add($"{fileId}:{trackId}");
                }
            }

            var titleArg = RemoveFileTitle ? "--title \"\" --no-global-tags" : string.Empty;
            var attachmentArg = RemoveAttachments ? "--no-attachments" : string.Empty;
            var trackOrderArg = trackOrder.Count > 0 ? $"--track-order {string.Join(',', trackOrder)}" : string.Empty;

            return $"-o \"{fileInfo.OutputFileName}\" {titleArg} {string.Join(' ', args)} {trackOrderArg} {attachmentArg}";
        }

        private void AddExternalAudioTrack(
            List<string> args,
            List<string> trackOrder,
            List<(int id, string lang)> orderedAudio,
            ExternalAudioTrack externalAudioFile,
            int externalFileIndex)
        {
            string ext = Path.GetExtension(externalAudioFile.FileName).ToLowerInvariant();
            string lang = !string.IsNullOrEmpty(externalAudioFile.LanguageCode) ? externalAudioFile.LanguageCode : "und";

            if (ext == ".mka")
            {
                string extJson = RunMkvMergeJson(externalAudioFile.FileName);
                using var extDoc = JsonDocument.Parse(extJson);
                var extTracks = extDoc.RootElement.GetProperty("tracks")
                    .EnumerateArray()
                    .Where(t => t.GetProperty("type").GetString() == "audio")
                    .ToList();

                foreach (var extTrack in extTracks)
                {
                    int trackId = extTrack.GetProperty("id").GetInt32();

                    string title = "";
                    if (UseAutoTitle)
                    {
                        var props = extTrack.GetProperty("properties");

                        string langName = GetLanguageName(lang);
                        string codecId = props.TryGetProperty("codec_id", out var cd) ? cd.GetString() ?? string.Empty : string.Empty;
                        int channels = props.TryGetProperty("audio_channels", out var ch) && ch.TryGetInt32(out var c) ? c : 0;

                        var mediaInfo = GetAudioInfo(externalAudioFile.FileName);
                        int bitrate = mediaInfo?.BitRate ?? 0;

                        string audioType = GetAudioType(codecId);
                        string channelDesc = FormatChannels(channels);
                        string bitrateDesc = bitrate > 0 ? $"{bitrate / 1000} kbps" : "unknown bitrate";

                        title = $"{langName} {audioType} {channelDesc} @ {bitrateDesc}";
                    }

                    args.Add($"--language {trackId}:{lang}");
                    args.Add($"--track-name {trackId}:\"{title}\"");
                    args.Add($"--default-track-flag {trackId}:no");
                    args.Add($"\"{externalAudioFile.FileName}\"");

                    orderedAudio.Add((trackId + 1000 * externalFileIndex, lang)); // unique ID for ordering
                    trackOrder.Add($"{externalFileIndex}:{trackId}");
                }
            }
            else
            {
                // Raw audio file fallback (AAC, AC3, DTS, etc.)
                int trackId = 0;

                string title = "";
                if (UseAutoTitle)
                {
                    var mediaInfo = GetAudioInfo(externalAudioFile.FileName);

                    string langName = GetLanguageName(lang);
                    string audioType = mediaInfo != null ? GetAudioTypeFromBitrateAndExtension(mediaInfo.BitRate, ext) : "Audio";
                    string channelDesc = mediaInfo != null ? FormatChannels(mediaInfo.Channels) : "";
                    string bitrateDesc = mediaInfo?.BitRate > 0 ? $"{mediaInfo.BitRate / 1000} kbps" : "unknown bitrate";

                    title = $"{langName} {audioType} {channelDesc} @ {bitrateDesc}";
                }

                args.Add($"--language {trackId}:{lang}");
                args.Add($"--track-name {trackId}:\"{title}\"");
                args.Add($"--default-track-flag {trackId}:no");
                args.Add($"\"{externalAudioFile.FileName}\"");

                orderedAudio.Add((trackId + 1000 * externalFileIndex, lang));
                trackOrder.Add($"{externalFileIndex}:{trackId}");
            }
        }


        private string GetLanguageName(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            { 
                return "Unknown";
            }

            code = code.Trim().ToLowerInvariant();
            
            var match = Languages.Iso639.FirstOrDefault(lang =>
                lang.Abr2.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                lang.Abr3a.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                lang.Abr3b.Equals(code, StringComparison.OrdinalIgnoreCase));
            
            if (match is null)
            {
                return code.ToUpperInvariant();
            }

            //temporary slution to remove Moldavian/Moldovan from the name
            return match.Name.Replace("; Moldavian; Moldovan", string.Empty);
        }

        private string GetAudioType(string codecId)
        {
            return codecId switch
            {
                "A_EAC3" => "DDP",
                "A_AC3" => "AC3",
                "A_AAC" => "AAC",
                "A_OPUS" => "Opus",
                "A_DTS" => "DTS",
                "A_TRUEHD" => "TrueHD",
                _ => "Audio"
            };
        }
        private string GetAudioTypeFromBitrateAndExtension(int? bitrate, string extension)
        {
            return extension switch
            {
                ".aac" => "AAC",
                ".ac3" => "AC3",
                ".eac3" => "E-AC3",
                ".dts" => "DTS",
                _ => bitrate == null ? string.Empty : bitrate >= 320000 ? "HQ Audio" : "Audio"
            };
        }


        private string FormatChannels(int ch)
        {
            return ch switch
            {
                1 => "1.0",
                2 => "2.0",
                6 => "5.1",
                8 => "7.1",
                _ => $"{ch}.0"
            };
        }

        public string RunRemux(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = MkvMergePath,
                Arguments = args,
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
            return args;
        }

        public async Task<(bool Success, string Output)> RunRemuxAsync(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = MkvMergePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            process.Start();
            outputBuilder.AppendLine(await process.StandardOutput.ReadToEndAsync());
            outputBuilder.AppendLine(await process.StandardError.ReadToEndAsync());
            await process.WaitForExitAsync();
            bool success = process.ExitCode == 0;
            return (success, outputBuilder.ToString());
        }
    }
}
