#include "ImperatorToCK3Converter.h"
#include "CK3Outputter/outVersion.h"
#include "Log.h"
#include "ConverterVersion.h"
#include <Magick++/Functions.h>



int main(const int argc, const char* argv[]) {
	try {
		Magick::InitializeMagick(nullptr);

		commonItems::ConverterVersionParser versionParser;
		const auto converterVersion = versionParser.importVersion("configurables/version.txt");
		logConverterVersion(converterVersion);
		if (argc >= 2) {
			Log(LogLevel::Info) << "ImperatorToCK3 takes no parameters.";
			Log(LogLevel::Info) << "It uses configuration.txt, configured manually or by the frontend.";
		}

		convertImperatorToCK3(converterVersion);
		return 0;
	}
	catch (const std::exception& e) {
		Log(LogLevel::Error) << e.what();
		return -1;
	}
}