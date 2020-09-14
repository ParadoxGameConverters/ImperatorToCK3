#include "outColoredEmblems.h"
#include <filesystem>
#include "Magick++.h"

#include "OSCompatibilityLayer.h"


void CK3::outputColoredEmblems(const Configuration& theConfiguration, const World& CK3World)
{
	const auto coloredEmblemsPath = theConfiguration.getImperatorPath() + "/game/gfx/coat_of_arms/colored_emblems/";
	auto filenames = Utils::GetAllFilesInFolder(coloredEmblemsPath);
	for (const auto& filename : filenames)
	{
		if (filename == "ce_lion.dds" && !Utils::DoesFileExist("output/" + CK3World.getOutputModName() + "/gfx/coat_of_arms/colored_emblems/ce_lion.dds")) // something's wrong with this fucking ce_lion.dds
		// instead of converting a broken file from Imperator, copy closest CK3 emblem
			Utils::TryCopyFile(theConfiguration.getCK3Path() + "/game/gfx/coat_of_arms/colored_emblems/ce_lion_passant.dds",
				"output/" + CK3World.getOutputModName() + "/gfx/coat_of_arms/colored_emblems/ce_lion.dds");
		else
		{
			// load an image
			Magick::Image image(coloredEmblemsPath + filename);
			image.negateChannel(MagickCore::ChannelType::RedChannel);
			// Write the image to new file
			image.write("output/" + CK3World.getOutputModName() + "/gfx/coat_of_arms/colored_emblems/" + filename);
		}
	}
}
