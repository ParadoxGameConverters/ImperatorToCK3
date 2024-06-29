using commonItems;

namespace ImperatorToCK3;

internal static class Converter {
	public static void ConvertImperatorToCK3(ConverterVersion converterVersion) {
		Logger.Progress(0);
		DebugInfo.LogEverything();
		SystemUtils.TryCreateFolder("temp");
		var config = new Configuration(converterVersion);

		var imperatorWorld = new Imperator.World(config, converterVersion);

		var ck3World = new CK3.World(imperatorWorld, config);
		Outputter.WorldOutputter.OutputWorld(ck3World, imperatorWorld, config);

		Logger.Info("* Conversion complete! *");
		Logger.Progress(100);
	}
}