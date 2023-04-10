using commonItems;
using ImperatorToCK3.Helpers;
using System.IO;
using System.Threading;

namespace ImperatorToCK3;

internal static class Converter {
	public static void ConvertImperatorToCK3(ConverterVersion converterVersion) {
		Logger.Progress(0);
		DebugInfo.LogEverything();
		SystemUtils.TryCreateFolder("temp");
		var config = new Configuration(converterVersion);
		
		var irMusicTokenSource = PlayImperatorMusic(config);
		var imperatorWorld = new Imperator.World(config, converterVersion);
		irMusicTokenSource.Cancel();
		
		var ck3MusicTokenSource = PlayCK3Music(config);
		var ck3World = new CK3.World(imperatorWorld, config);
		Outputter.WorldOutputter.OutputWorld(ck3World, imperatorWorld, config);
		Logger.Info("* Conversion complete! *");
		Logger.Progress(100);
		ck3MusicTokenSource.Cancel();
	}

	private static CancellationTokenSource PlayImperatorMusic(Configuration config) {
		var  tokenSource = new CancellationTokenSource();
		CancellationToken token = tokenSource.Token;
		
		var irMusicPath = Path.Combine(config.ImperatorPath, "game/sound/banks/Music.bank");
		MusicPlayer.PlayMusic(irMusicPath, token);

		return tokenSource;
	}
	
	private static CancellationTokenSource PlayCK3Music(Configuration config) {
		var  tokenSource = new CancellationTokenSource();
		CancellationToken token = tokenSource.Token;
		
		var ck3MusicPath = Path.Combine(config.CK3Path, "game/sound/banks/Soundtrack.bank");
		MusicPlayer.PlayMusic(ck3MusicPath, token);

		return tokenSource;
	}
}
