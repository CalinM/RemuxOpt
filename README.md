# Remuxed File Behavior:
Each file is analyzed independently, and a unique set of mkvmerge arguments is generated based on its specific tracks and the configured preferences.


The new MKV files will:
- Include only the audio and subtitle tracks whose languages are listed in the respective language order lists. The track order in the output file will match the order of these lists, which can be adjusted via drag & drop.
- Prioritize stereo (2.0) audio over surround (5.1) when multiple tracks exist for the same language. For example, if an MKV contains two English audio tracks (5.1 and 2.0), the remuxed file will place the 2.0 track first and the 5.1 second. This rule applies to all configured audio languages.
- Enable all tracks
- The default track can be specified by right clicking a configured audio track and set it as default (no matter the order). If no track is set as "default" the muxin process will set the first track (in the configured order) as default
- Automatically generate audio track titles based on technical details (e.g., English DD 2.0 @ 192 kbps), if the "Auto-title" option is enabled. If disabled, audio track titles will be removed.
- Preserve subtitle track titles (e.g., “CC”, “SDH”) by default, unless explicitly configured otherwise.
- Preserve chapter information.

Optional Processing (if enabled):
- Remove the video track title (if configured).
- Clear all "forced track" flags from both audio and subtitle tracks.
- Remove all attachments (e.g., fonts or images embedded in the MKV).
- Reset the MKV container title (clearing it entirely).

Basic support for external files:
- if a file with the same name as the mkv file + language code suffix exists, it will be treated as external audio track and it ull be remuxed according with the audio track order

	FileName1.mkv
	FileName1_ro.aac
	
  If the language code is not present or not configured ...
  
  FileName2.mkv
	FileName2.dts

  the audio track is remuxed, but the external audio track has "und" language and is placed last.

- the supported audio files types are: aac, ac3, dts, mka, eac3




## Example
Let’s consider two MKV files with the following audio tracks:

### Original – File 1:
  - Dutch (Nl)
  - Romanian (Ro) – 2.0
  - English (En) – 2.0
  - Romanian (Ro) – 5.1
  - English (En) – 5.1

  
### Original – File 2:
  - English (En) – 5.1
  - Dutch (Nl)
  - Romanian (Ro) – 2.0
  - English (En) – 2.0
  - Romanian (Ro) – 5.1

Even though the original order differs, both files will be remuxed into the same standardized track sequence based on the configured language priority and stereo-before-surround rule:

### Remuxed Output:
  - Romanian (Ro) – 2.0
  - Romanian (Ro) – 5.1
  - Dutch (Nl)
  - English (En) – 2.0
  - English (En) – 5.1

This guarantees consistent audio track positioning across all remuxed files, which is especially useful when working with batches of episodes or multilingual content.


