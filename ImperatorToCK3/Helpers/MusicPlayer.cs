using commonItems;
using Fmod5Sharp;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace ImperatorToCK3.Helpers; 

public static class MusicPlayer {
	public static void PlayMusic(string filePath, CancellationToken cancellationToken) {
		var thread = new Thread(() => {
			try {
				PlayMusicInternal(filePath, cancellationToken);
			} catch (Exception e) {
				Logger.Warn($"Failed to play music file \"{filePath}\"! {e}");
			}
		}) {IsBackground = true};
		thread.Start();
	}

	private static void PlayMusicInternal(string filePath, CancellationToken cancellationToken) {
		if (!File.Exists(filePath)) {
			throw new FileNotFoundException($"Music file \"{filePath}\" not found!");
		}

		var extension = CommonFunctions.GetExtension(filePath);
		if (CommonFunctions.GetExtension(filePath) == "bank") {
			// Extract music from bank.
			Logger.Debug("Loading bank...");
			var bytes = File.ReadAllBytes(filePath);
			var index = bytes.AsSpan().IndexOf("FSB5"u8);
			if (index > 0) {
				bytes = bytes.AsSpan(index).ToArray();
			}

			var bank = FsbLoader.LoadFsbFromByteArray(bytes);

			Logger.Debug("Extracting music from bank...");
			var bankSample = bank.Samples.First(s => s.Name is not null);
			var name = bankSample.Name!;
			if (!bankSample.RebuildAsStandardFileFormat(out var data, out var extractedFileExtension)) {
				Logger.Warn($"Failed to extract bank sample {name}!");
				return;
			}
			
			if (!Directory.Exists("temp")) {
				Directory.CreateDirectory("temp");
			}
			var extractedFilePath = $"temp/extracted_{name}.{extractedFileExtension}";
			File.WriteAllBytes(extractedFilePath, data);
			Logger.Debug($"Extracted sample {name}.");
			
			filePath = extractedFilePath;
			extension = extractedFileExtension;
		}

		using var waveOut = new WaveOutEvent();
		switch (extension) {
			case "mp3":
			case "wav": {
				using var waveProvider = new AudioFileReader(filePath);
				waveOut.Init(waveProvider);
				break;
			}
			case "ogg": {
				using var waveProvider = new NAudio.Vorbis.VorbisWaveReader(filePath);
				waveOut.Init(waveProvider);
				break;
			}
			default:
				throw new FormatException($"Unsupported music file format: {extension}!");
		}

		waveOut.Volume = 0.5f;
		waveOut.Play();
		while (!cancellationToken.IsCancellationRequested && waveOut.PlaybackState == PlaybackState.Playing) {
			// Wait for playback to finish.
			Thread.Sleep(1000);
		}
	}
}