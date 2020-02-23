# Timecode Util

Timecode util is a small utility for timecode file processing written in C#.

## Platform

- .NET Framework 4.5
- .NET Core 3.1

## The library

The core library of Timecode Util allows you load timecode (v1 or v2) from file or a `TextReader`. The accuracy is auto fixed when loading. You can change total frames of the file, query the frame number or timestamps from each other and re-save the file with the format you like.

## The CLI

The command line interface is a simple wrapping of the core library. You can show the general information, convert between versions and query frame numbers or timestamps.

The general usage of the CLI is `TorrentUtil INPUT ACTION [...]`, where a action could be `info`, `convert` or `query`. The ability of input or output from/to a pipe is also supported.

Details can be found in the tool itself.

## License

Code licensed under the [MIT License](LICENSE.txt).
