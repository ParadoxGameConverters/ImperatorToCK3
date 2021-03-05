#include "outColoredEmblems.h"
#include "Configuration/Configuration.h"
#include "OSCompatibilityLayer.h"
#include "Magick++.h"
#include <filesystem>



namespace CK3 {

bool isBrokenEmblem(const Configuration& theConfiguration, const std::string& outputName, const std::string& filename)
{
	if (filename == "ce_lion.dds" && !commonItems::DoesFileExist("output/" + outputName + "/gfx/coat_of_arms/colored_emblems/ce_lion.dds")) { // something's wrong with this fucking ce_lion.dds
		// instead of converting a broken file from Imperator, copy closest CK3 emblem
		commonItems::TryCopyFile(theConfiguration.getCK3Path() + "/game/gfx/coat_of_arms/colored_emblems/ce_lion_passant.dds",
								"output/" + outputName + "/gfx/coat_of_arms/colored_emblems/ce_lion.dds");
		return true;
	}
	return false;
}


void copyColoredEmblems(const Configuration& theConfiguration, const std::string& outputName) {
	const auto coloredEmblemsPath = theConfiguration.getImperatorPath() + "/game/gfx/coat_of_arms/colored_emblems";
	auto filenames = commonItems::GetAllFilesInFolderRecursive(coloredEmblemsPath);
	for (const auto& filename : filenames) {
		if (isBrokenEmblem(theConfiguration, outputName, filename))
			continue;
		// load an image
		Magick::Image image(coloredEmblemsPath + "/" + filename);
		image.negateChannel(MagickCore::ChannelType::RedChannel);
		// Write the image to new file
		image.write("output/" + outputName + "/gfx/coat_of_arms/colored_emblems/" + filename);
	}
}

} // namespace CK3