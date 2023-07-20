using commonItems;
using Fmod5Sharp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ImperatorToCK3.Helpers; 

public static class MusicPlayer {
	public static void PlayMusic(IEnumerable<string> filePaths) {
		var thread = new Thread(() => {
			try {
				PlayMusicInternal(filePaths);
			} catch (Exception e) {
				Logger.Warn($"Failed to play music! {e}");
			}
		}) {IsBackground = true};
		thread.Start();
	}

	private static void PlayMusicInternal(IEnumerable<string> filePaths) {
		var waveProviders = filePaths.Select(GetWaveProvider).ToList();
		var playlist = new ConcatenatingSampleProvider(waveProviders);
		
		using var waveOut = new WaveOutEvent();
		waveOut.Volume = 0.4f;
		waveOut.Init(playlist);
		waveOut.Play();
		
		while (waveOut.PlaybackState == PlaybackState.Playing) {
			// Wait for playback to finish.
			Thread.Sleep(1000);
		}
	}

	public static IEnumerable<string> ExtractSamplesFromBank(string bankFilePath, IEnumerable<string> sampleNames) {
		var fileNamesToReturn = new List<string>();
		
		try {
			Logger.Debug($"Loading bank {bankFilePath}...");
			var bytes = File.ReadAllBytes(bankFilePath);
			var index = bytes.AsSpan().IndexOf("FSB5"u8);
			if (index > 0) {
				bytes = bytes.AsSpan(index).ToArray();
			}

			var bank = FsbLoader.LoadFsbFromByteArray(bytes);
			var baseFileNameWithoutExtension = Path.GetFileNameWithoutExtension(bankFilePath);
			foreach (var sample in bank.Samples.Where(s => sampleNames.Contains(s.Name))) {
				var name = sample.Name!;
			
				if (!sample.RebuildAsStandardFileFormat(out var data, out var extension)) {
					Logger.Warn($"Failed to extract bank sample {name}!");
					continue;
				}

				var extractedFilePath = $"temp/music_{baseFileNameWithoutExtension}_{name}.{extension}";
				File.WriteAllBytes(extractedFilePath, data);
			
				fileNamesToReturn.Add(extractedFilePath);
			}
		} catch (Exception e) {
			Logger.Error($"Failed to extract samples from bank file \"{bankFilePath}\"! {e}");
		}

		return fileNamesToReturn;
	}
	
	private static ISampleProvider GetWaveProvider(string filePath) {
		var extension = CommonFunctions.GetExtension(filePath);
		switch (extension) {
			case "mp3":
			case "wav": {
				return new AudioFileReader(filePath);
			}
			case "ogg": {
				return new NAudio.Vorbis.VorbisWaveReader(filePath);
			}
			default:
				throw new FormatException($"Unsupported music file format: {extension}!");
		}
	}
}