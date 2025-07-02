using System.Diagnostics;
using System.Text.Json;

namespace RemuxOpt
{
    public class MkvMetadataExtractor
    {
        public async Task<MkvFileInfo> ExtractInfoAsync(string filePath)
        {
            string mkvJson = RunMkvMergeJson(filePath);
            using var mkvDoc = JsonDocument.Parse(mkvJson);
            var tracks = mkvDoc.RootElement.GetProperty("tracks");

            string ffJson = RunFfprobeJson(filePath);
            using var ffDoc = JsonDocument.Parse(ffJson);

            var result = new MkvFileInfo
            {
                FileName = filePath
            };

            // Audio Tracks

            var bitrateMap = new Dictionary<int, int>();

            foreach (var stream in ffDoc.RootElement.GetProperty("streams").EnumerateArray())
            {
                if (stream.GetProperty("codec_type").GetString() != "audio")
                    continue;

                int idx = stream.GetProperty("index").GetInt32();

                int bitrate = 0;

                // Try "bit_rate" directly
                if (stream.TryGetProperty("bit_rate", out var brProp))
                {
                    if (brProp.ValueKind == JsonValueKind.Number && brProp.TryGetInt32(out int br))
                    {
                        bitrate = br;
                    }
                    else if (brProp.ValueKind == JsonValueKind.String && int.TryParse(brProp.GetString(), out br))
                    {
                        bitrate = br;
                    }
                }
                else if (stream.TryGetProperty("tags", out var tags) &&
                         tags.TryGetProperty("BPS", out var bpsProp))
                {
                    if (bpsProp.ValueKind == JsonValueKind.Number && bpsProp.TryGetInt32(out int bps))
                    {
                        bitrate = bps;
                    }
                    else if (bpsProp.ValueKind == JsonValueKind.String && int.TryParse(bpsProp.GetString(), out bps))
                    {
                        bitrate = bps;
                    }
                }

                if (bitrate > 0)
                {
                    bitrateMap[idx] = bitrate;
                }
            }

            // Process audio tracks
            foreach (var t in tracks.EnumerateArray().Where(t => t.GetProperty("type").GetString() == "audio"))
            {
                var id = t.GetProperty("id").GetInt32();
                var props = t.GetProperty("properties");
                string lang = props.TryGetProperty("language", out var lp) ? lp.GetString() ?? "und" : "und";
                string codecId = props.TryGetProperty("codec_id", out var cd) ? cd.GetString() ?? "" : "";
                int channels = props.TryGetProperty("audio_channels", out var ch) && ch.TryGetInt32(out var c) ? c : 0;
                int bitRate = bitrateMap.TryGetValue(id, out var br2) ? br2 : 0;
                bool isForced = props.TryGetProperty("forced_track", out var forced) &&
                              (forced.ValueKind == JsonValueKind.True ||
                               (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1));

                result.AudioTracks.Add(new AudioTrackInfo
                {
                    FileId = 0,
                    TrackId = id,
                    Language = lang,
                    CodecId = codecId,
                    Channels = channels,
                    BitRate = bitRate,
                    IsForced = isForced,
                    FileName = filePath
                });
            }

            var mkvBaseName = Path.GetFileNameWithoutExtension(filePath);
            var folder = Path.GetDirectoryName(filePath);

            result.ExternalAudioTracks = FindExternalAudioTracks(folder, mkvBaseName);

            // Subtitles
            foreach (var t in tracks.EnumerateArray().Where(t => t.GetProperty("type").GetString() == "subtitles"))
            {
                int id = t.GetProperty("id").GetInt32();
                var props = t.GetProperty("properties");
                string lang = props.TryGetProperty("language", out var lp) ? lp.GetString() ?? "und" : "und";
                bool isForced = props.TryGetProperty("forced_track", out var forced) &&
                              (forced.ValueKind == JsonValueKind.True ||
                               (forced.ValueKind == JsonValueKind.Number && forced.GetInt32() == 1));

                result.SubtitleTracks.Add(new SubtitleTrackInfo
                {
                    FileId = 0,
                    TrackId = id,
                    Language = lang,
                    IsForced = isForced,
                    FileName = filePath
                });
            }

            if (mkvDoc.RootElement.TryGetProperty("attachments", out var attachments) &&
                attachments.ValueKind == JsonValueKind.Array)
            {
                foreach (var att in attachments.EnumerateArray())
                {
                    var mimeType = att.TryGetProperty("content_type", out var ct) ? ct.GetString() ?? "" : "";
                    var fileName = att.TryGetProperty("file_name", out var fn) ? fn.GetString() ?? "" : "";

                    result.Attachments.Add(new Attachment
                    {
                        MimeType = mimeType,
                        FileName = fileName
                    });
                }
            }

            return result;
        }

        private string RunMkvMergeJson(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Common.MkvMergePath,
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

        /// <summary>
        /// MkvMerge JSON output is not always reliable for audio tracks, especially for bitrate and number of channels values.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string RunFfprobeJson(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Common.FfprobePath,
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

        /// <summary>
        /// The external files (expecially aac) are not always detected by ffprobe, so we use MediaInfo to get the audio track info.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private AudioTrackInfo GetAudioUsingMediaInfo(string filePath)
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

        private List<AudioTrackInfo> FindExternalAudioTracks(string folder, string baseName)
        {
            var result = new List<AudioTrackInfo>();

            var externalFileIndex = 1;
            var audioExtensions = new[] { ".aac", ".ac3", ".dts", ".mka", ".eac3", ".m4a" };

            var externalAudioFiles =
                Directory.EnumerateFiles(folder).Where(path =>
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    var name = Path.GetFileNameWithoutExtension(path);
                    return audioExtensions.Contains(ext) && name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
                }).ToList();

            foreach (var filePath in externalAudioFiles)
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var lang = TryGuessLanguageCodeFromFileName(filePath) ?? "und";

                var mediaInfo = GetAudioUsingMediaInfo(filePath);

                if (ext == ".mka")
                {
                    string extJson = RunMkvMergeJson(filePath);
                    using var extDoc = JsonDocument.Parse(extJson);
                    var extTracks = extDoc.RootElement.GetProperty("tracks")
                        .EnumerateArray()
                        .Where(t => t.GetProperty("type").GetString() == "audio");



                    foreach (var extTrack in extTracks)
                    {
                        int trackId = extTrack.GetProperty("id").GetInt32();
                        var props = extTrack.GetProperty("properties");
                        string codecId = props.TryGetProperty("codec_id", out var cd) ? cd.GetString() ?? "" : "";
                        int channels = props.TryGetProperty("audio_channels", out var ch) && ch.TryGetInt32(out var c) ? c : 0;
                        int bitRate = mediaInfo?.BitRate ?? 0;

                        result.Add(new AudioTrackInfo
                        {
                            FileId = externalFileIndex,
                            TrackId = trackId,
                            Language = lang,
                            CodecId = codecId,
                            Channels = channels,
                            BitRate = bitRate,
                            IsForced = false, // External audio files typically don't have forced flags
                            FileName = filePath
                        });
                    }
                }
                else
                {
                    result.Add(new AudioTrackInfo
                    {
                        FileId = externalFileIndex,
                        TrackId = 0,
                        Language = lang,
                        Channels = mediaInfo?.Channels ?? 0,
                        BitRate = mediaInfo?.BitRate ?? 0,
                        IsForced = false, // External audio files typically don't have forced flags
                        FileName = filePath
                    });
                }

                externalFileIndex++;
            }

            return result;
        }

        private string? TryGuessLanguageCodeFromFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Extract suffix after base name, if present
            var langCandidate = fileName
                .Split(['_', '.', '-', ' '], StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault()?.ToLowerInvariant();

            return Languages.Iso639.Select(l => l.Abr3a).Contains(langCandidate) ? langCandidate : null;
        }
    }
}
