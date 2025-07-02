# Release Notes - Version 1.0.0.147

**Release Date:** 2025-07-02

## 🚀 New Features
- new option was added to force remove the tracks not configured in the order lists feat: added a new option to bulk update the language on the selected files tracks (right click on the columns header) feat: moved the language selector into a standalone usercontrol feat: removed the double mkv (source) processing (one to show data and a 2nd one while remuxing). All the data needed for remux is now determined while loading the files properties., feat: added two new options: to configure a default subtitle and to specify no default for subtitles feat: removed the "ExternalAudioTracks" internal concept. Everything is hadled trough AudioTrackInfo feat: the grid is now refreshed after updates of the languages tracks order (adding the missing language code). fix: the loading process no longer crashes when not all files have external audio tracks fix: the data provided to the grid columns: some hidden column were not getting the values, causing the data to shift fix: the output file extension is now always mkv (and not like before, when it was taking the source extension) [11272a0]
- Added a new category of options to apply certain file names conventions (ongoing). [285d690]
- Added Options form with two new settings:  - Read files from all subfolders recursively  - Delete the original file if no errors are encountered [ced0de6]

## 🐛 Bug Fixes
- Fixed an issue during the determination of the source tracks languages codes. [93a1dee]

## 🔧 Other Changes
- Update release.yml [c443c04]
- Update release.yml [b0d79d6]

---
**Full Changelog:** v1.0.0.15...1.0.0.147
**Commits in this release:** 6
