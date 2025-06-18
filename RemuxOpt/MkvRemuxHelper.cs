using System.Diagnostics;
using System.Text.Json;

namespace RemuxOpt
{
    public class MkvRemuxHelper
    {
        public bool UseAutoTitle { get; set; }
        public bool RemoveAttachments { get; set; }
        public bool RemoveForcedFlags { get; set; }
        public List<string> AudioLanguageOrder { get; set; } = new();
        public List<string> SubtitleLanguageOrder { get; set; } = new();

        public string MkvMergePath { get; set; } = "mkvmerge";

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

            using var process = Process.Start(startInfo);
            using var reader = process.StandardOutput;
            string output = reader.ReadToEnd();
            process.WaitForExit();

            return output;
        }

        //public string BuildMkvMergeArgs(string inputFile, string outputFile)
        //{
        //    string json = RunMkvMergeJson(inputFile);
        //    using var doc = JsonDocument.Parse(json);

        //    var tracks = doc.RootElement.GetProperty("tracks");

        //    var audioTracks = tracks.EnumerateArray()
        //        .Where(t => t.GetProperty("type").GetString() == "audio")
        //        .ToList();

        //    var subtitleTracks = tracks.EnumerateArray()
        //        .Where(t => t.GetProperty("type").GetString() == "subtitles")
        //        .ToList();

        //    List<string> args = new();
        //    List<string> trackOrder = new();
        //    int fileId = 0;

        //    List<(int id, string lang)> orderedAudio = new();
        //    List<(int id, string lang)> orderedSubtitles = new();

        //    foreach (var lang in AudioLanguageOrder)
        //    {
        //        var matches = audioTracks.Where(t => (t.GetProperty("properties").GetProperty("language").GetString() ?? "und") == lang);
        //        foreach (var t in matches)
        //        {
        //            orderedAudio.Add((t.GetProperty("id").GetInt32(), lang));
        //        }
        //    }

        //    foreach (var lang in SubtitleLanguageOrder)
        //    {
        //        var matches = subtitleTracks.Where(t => (t.GetProperty("properties").GetProperty("language").GetString() ?? "und") == lang);
        //        foreach (var t in matches)
        //        {
        //            orderedSubtitles.Add((t.GetProperty("id").GetInt32(), lang));
        //        }
        //    }

        //    // AUDIO
        //    for (int i = 0; i < orderedAudio.Count; i++)
        //    {
        //        var (trackId, lang) = orderedAudio[i];

        //        args.Add($"--audio-tracks {trackId}");
        //        args.Add($"--language {trackId}:{lang}");
        //        args.Add($"--track-name {trackId}:\"{(UseAutoTitle ? $"{lang.ToUpper()} {i + 1}" : "")}\"");
        //        args.Add($"--default-track-flag {trackId}:{(i == 0 ? "yes" : "no")}");

        //        trackOrder.Add($"{fileId}:{trackId}");
        //    }

        //    // SUBTITLES
        //    foreach (var (trackId, lang) in orderedSubtitles)
        //    {
        //        args.Add($"--subtitle-tracks {trackId}");
        //        args.Add($"--language {trackId}:{lang}");
        //        args.Add($"--sub-charset {trackId}:UTF-8");

        //        if (RemoveForcedFlags)
        //        {
        //            var track = subtitleTracks.First(t => t.GetProperty("id").GetInt32() == trackId);
        //            if (track.GetProperty("properties").TryGetProperty("forced_track", out var forced))
        //            {
        //                if ((forced.ValueKind == JsonValueKind.True) || (forced.ValueKind == JsonValueKind.False && forced.GetBoolean()) || (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1))
        //                {
        //                    args.Add($"--forced-track {trackId}:no");
        //                }
        //            }
        //        }

        //        trackOrder.Add($"{fileId}:{trackId}");
        //    }

        //    string attachmentArg = RemoveAttachments ? "--no-attachments" : "";
        //    string trackOrderArg = trackOrder.Count > 0 ? $"--track-order {string.Join(",", trackOrder)}" : "";

        //    return $"-o \"{outputFile}\" {string.Join(" ", args)} {trackOrderArg} {attachmentArg} \"{inputFile}\"";
        //}

        public string BuildMkvMergeArgs(string inputFile, string outputFile)
        {
            string json = RunMkvMergeJson(inputFile);
            using var doc = JsonDocument.Parse(json);

            var tracks = doc.RootElement.GetProperty("tracks");

            var audioTracks = tracks.EnumerateArray()
                .Where(t => t.GetProperty("type").GetString() == "audio")
                .ToList();

            var subtitleTracks = tracks.EnumerateArray()
                .Where(t => t.GetProperty("type").GetString() == "subtitles")
                .ToList();

            List<string> args = new();
            List<string> trackOrder = new();
            int fileId = 0;

            List<(int id, string lang)> orderedAudio = new();
            List<(int id, string lang)> orderedSubtitles = new();

            foreach (var lang in AudioLanguageOrder)
            {
                var matches = audioTracks
                    .Where(t => (t.GetProperty("properties").GetProperty("language").GetString() ?? "und") == lang);
                foreach (var t in matches)
                {
                    orderedAudio.Add((t.GetProperty("id").GetInt32(), lang));
                }
            }

            foreach (var lang in SubtitleLanguageOrder)
            {
                var matches = subtitleTracks
                    .Where(t => (t.GetProperty("properties").GetProperty("language").GetString() ?? "und") == lang);
                foreach (var t in matches)
                {
                    orderedSubtitles.Add((t.GetProperty("id").GetInt32(), lang));
                }
            }

            // Merge audio track IDs into one --audio-tracks
            if (orderedAudio.Count > 0)
            {
                args.Add($"--audio-tracks {string.Join(",", orderedAudio.Select(x => x.id))}");

                for (int i = 0; i < orderedAudio.Count; i++)
                {
                    var (trackId, lang) = orderedAudio[i];
                    args.Add($"--language {trackId}:{lang}");
                    args.Add($"--track-name {trackId}:\"{(UseAutoTitle ? $"{lang.ToUpper()} {i + 1}" : "")}\"");
                    args.Add($"--default-track-flag {trackId}:{(i == 0 ? "yes" : "no")}");
                    trackOrder.Add($"{fileId}:{trackId}");
                }
            }

            // Merge subtitle track IDs into one --subtitle-tracks
            if (orderedSubtitles.Count > 0)
            {
                args.Add($"--subtitle-tracks {string.Join(",", orderedSubtitles.Select(x => x.id))}");

                foreach (var (trackId, lang) in orderedSubtitles)
                {
                    args.Add($"--language {trackId}:{lang}");
                    args.Add($"--sub-charset {trackId}:UTF-8");

                    if (RemoveForcedFlags)
                    {
                        var track = subtitleTracks.First(t => t.GetProperty("id").GetInt32() == trackId);
                        if (track.GetProperty("properties").TryGetProperty("forced_track", out var forced))
                        {
                            if ((forced.ValueKind == JsonValueKind.True) ||
                                (forced.ValueKind == JsonValueKind.False && forced.GetBoolean()) ||
                                (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1))
                            {
                                args.Add($"--forced-track {trackId}:no");
                            }
                        }
                    }

                    trackOrder.Add($"{fileId}:{trackId}");
                }
            }

            string attachmentArg = RemoveAttachments ? "--no-attachments" : "";
            string trackOrderArg = trackOrder.Count > 0 ? $"--track-order {string.Join(",", trackOrder)}" : "";

            return $"-o \"{outputFile}\" {string.Join(" ", args)} {trackOrderArg} {attachmentArg} \"{inputFile}\"";
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
    }
}
