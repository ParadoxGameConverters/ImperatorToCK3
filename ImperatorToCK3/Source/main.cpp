#include <Magick++/Functions.h>
#include "CK3Outputter/outVersion.h"
#include "ImperatorToCK3Converter.h"
#include "Log.h"



int main(const int argc, const char* argv[]) {
	try {
		Magick::InitializeMagick(nullptr);

		commonItems::ConverterVersion converterVersion;
		converterVersion.loadVersion("configurables/version.txt");
		logConverterVersion(converterVersion);
		if (argc >= 2) {
			Log(LogLevel::Info) << "ImperatorToCK3 takes no parameters.";
			Log(LogLevel::Info) << "It uses configuration.txt, configured manually or by the frontend.";
		}

		convertImperatorToCK3(converterVersion);
		return 0;
	} catch (const std::exception& e) {
		Log(LogLevel::Error) << e.what();
		return -1;
	}
}