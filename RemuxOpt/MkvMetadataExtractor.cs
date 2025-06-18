using System.Diagnostics;
using System.Text.Json.Nodes;

namespace RemuxOpt
{
    public class MkvFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public List<AudioTrack> AudioTracks { get; set; } = [];
        public List<SubtitleTrack> Subtitles { get; set; } = [];
        public List<Attachment> Attachments { get; set; } = [];
    }

    public class AudioTrack
    {
        public string Language { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Channels { get; set; }
        public int? Bitrate { get; set; }  // from ffprobe
    }

    public class SubtitleTrack
    {
        public string Language { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class Attachment
    {
        public string MimeType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public static class MkvMetadataExtractor
    {
        public static async Task<MkvFileInfo> ExtractInfoAsync(string filePath)
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

                result.AudioTracks.Add(new AudioTrack
                {
                    Language = lang,
                    Title = title,
                    Channels = channels
                });
            }

            // Get bitrate from ffprobe
            var ffAudio = ffObj["streams"].AsArray().Where(s => s["codec_type"]?.ToString() == "audio").ToList();
            for (int i = 0; i < result.AudioTracks.Count && i < ffAudio.Count; i++)
            {
                var bitrateStr = ffAudio[i]["bit_rate"]?.ToString();
                if (int.TryParse(bitrateStr, out int bitrate))
                    result.AudioTracks[i].Bitrate = bitrate;
            }

            // Subtitles
            foreach (var track in mkvObj["tracks"].AsArray().Where(t => t["type"].ToString() == "subtitles"))
            {
                var props = track["properties"];
                var lang = props?["language_ietf"]?.ToString() ?? props?["language"]?.ToString();
                var title = props?["track_name"]?.ToString();

                result.Subtitles.Add(new SubtitleTrack
                {
                    Language = lang,
                    Title = title
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

        private static async Task<string> RunProcessAsync(string fileName, string args)
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
