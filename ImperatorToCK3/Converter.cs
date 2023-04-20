using commonItems;
using ImperatorToCK3.Helpers;
using System.IO;
using System.Linq;

namespace ImperatorToCK3;

internal static class Converter {
	public static void ConvertImperatorToCK3(ConverterVersion converterVersion) {
		Logger.Progress(0);
		SystemUtils.TryCreateFolder("temp");
		var config = new Configuration(converterVersion);
		
		PlayMusic(config);
		
		var imperatorWorld = new Imperator.World(config, converterVersion);

		var ck3World = new CK3.World(imperatorWorld, config);
		Outputter.WorldOutputter.OutputWorld(ck3World, imperatorWorld, config);
		
		Logger.Info("* Conversion complete! *");
		Logger.Progress(100);
	}

	private static void PlayMusic(Configuration config) {
		var irBankPath = Path.Combine(config.ImperatorPath, "game/sound/banks/Music.bank");
		var irSampleNames = new[] {"PDX_IR_HG_MomentOfTheBattle_201102_v1_R01"};
		var irMusicPaths = MusicPlayer.ExtractSamplesFromBank(irBankPath, irSampleNames);
		
		var ck3BankPath = Path.Combine(config.CK3Path, "game/sound/banks/Soundtrack.bank");
		var ck3SampleNames = new[] {
			"taberna_revisited_in_game_instrumental",
			"thesacredrite_in_game",
			"charge_of_the_knights_final",
			"title_theme_the_dynasty_finalv2",
			"lionheart_withvocals_in_game"
		};
		var ck3MusicPaths = MusicPlayer.ExtractSamplesFromBank(ck3BankPath, ck3SampleNames);
		
		var musicPaths = irMusicPaths.Concat(ck3MusicPaths);
		MusicPlayer.PlayMusic(musicPaths);
	}
}
