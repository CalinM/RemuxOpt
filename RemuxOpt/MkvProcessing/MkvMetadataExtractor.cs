using System.Diagnostics;
using System.Text.Json.Nodes;

namespace RemuxOpt
{
    public class MkvMetadataExtractor
    {
        public async Task<MkvFileInfo> ExtractInfoAsync(string filePath)
        {
            var mkvmergeJson = await RunProcessAsync("mkvmerge", $"-J \"{filePath}\"");
            var ffprobeJson = await RunProcessAsync("ffprobe", $"-v quiet -print_format json -show_streams \"{filePath}\"");

            var mkvObj = JsonNode.Parse(mkvmergeJson);
            var ffObj = JsonNode.Parse(ffprobeJson);

            var result = new MkvFileInfo
            {
                FileName = filePath
            };

            // Audio Tracks
            foreach (var track in mkvObj["tracks"].AsArray().Where(t => t["type"].ToString() == "audio"))
            {
                var props = track["properties"];
                var lang = props?["language_ietf"]?.ToString() ?? props?["language"]?.ToString();
                var title = props?["track_name"]?.ToString();
                var channels = props?["audio_channels"]?.GetValue<int>() ?? 0;
                var isForced = props?["forced_track"] != null && bool.TryParse(props["forced_track"]?.ToString(), out var f) ? f : false;

                result.AudioTracks.Add(new AudioTrackInfo
                {
                    Language = lang,
                    Title = title,
                    Channels = channels,
                    IsForced = isForced,
                });
            }

            // Get bitrate from ffprobe
            var ffAudio = ffObj["streams"].AsArray().Where(s => s["codec_type"]?.ToString() == "audio").ToList();
            for (int i = 0; i < result.AudioTracks.Count && i < ffAudio.Count; i++)
            {
                var stream = ffAudio[i];
                var bitrate = 0;

                // Try direct "bit_rate"
                var bitrateStr = stream["bit_rate"]?.ToString();

                if (!int.TryParse(bitrateStr, out bitrate))
                {
                    // Fallback to tags.BPS
                    var tags = stream["tags"] as JsonObject;
                    var bpsStr = tags?["BPS"]?.ToString();
                    
                    _ = int.TryParse(bpsStr, out bitrate);
                }

                result.AudioTracks[i].BitRate = bitrate;
            }

            var mkvBaseName = Path.GetFileNameWithoutExtension(filePath);
            var folder = Path.GetDirectoryName(filePath);
            
            result.ExternalAudioFiles = FindExternalAudioTracks(folder, mkvBaseName);                

            // Subtitles
            foreach (var track in mkvObj["tracks"].AsArray().Where(t => t["type"].ToString() == "subtitles"))
            {
                var props = track["properties"];
                var lang = props?["language_ietf"]?.ToString() ?? props?["language"]?.ToString();
                var title = props?["track_name"]?.ToString();
                var isForced = props?["forced_track"] != null && bool.TryParse(props["forced_track"]?.ToString(), out var f) ? f : false;

                result.Subtitles.Add(new SubtitleTrackInfo
                {
                    Language = lang,
                    Title = title,
                    IsForced = isForced
                });
            }

            // Attachments
            foreach (var att in mkvObj["attachments"].AsArray())
            {
                result.Attachments.Add(new Attachment
                {
                    MimeType = att["content_type"]?.ToString(),
                    FileName = att["file_name"]?.ToString()
                });
            }

            return result;
        }

        private List<ExternalAudioTrack> FindExternalAudioTracks(string folder, string baseName)
        {
            var audioExtensions = new[] { ".aac", ".ac3", ".dts", ".mka", ".eac3" };

            return Directory.EnumerateFiles(folder)
                .Where(path =>
                    {
                        var ext = Path.GetExtension(path).ToLowerInvariant();
                        var name = Path.GetFileNameWithoutExtension(path);
                        return audioExtensions.Contains(ext) && name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
                    })
                .Select(filePath => new ExternalAudioTrack
                    {
                        Extension = Path.GetExtension(filePath),
                        FileName = filePath,
                        LanguageCode = TryGuessLanguageCodeFromFileName(filePath) ?? "und"
                    })
                .ToList();
        }

        private string? TryGuessLanguageCodeFromFileName(string filePath)
        {
            //var knownLangs = new[] { "eng", "dut", "ron", "fil", "fre", "ger", "ita", "spa", "jpn" };
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Extract suffix after base name, if present
            var langCandidate = fileName
                .Split(new[] { '_', '.', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault()?.ToLowerInvariant();

            return Languages.Iso639.Select(l => l.Abr3a).Contains(langCandidate) ? langCandidate : null;
        }

        private async Task<string> RunProcessAsync(string fileName, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"Error running {fileName}: {error}");

            return output;
        }
    }
}
