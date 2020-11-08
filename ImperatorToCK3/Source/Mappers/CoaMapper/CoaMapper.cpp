#include "CoaMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "OSCompatibilityLayer.h"

mappers::CoaMapper::CoaMapper(const Configuration& theConfiguration)
{
	const auto coasPath = theConfiguration.getImperatorPath() + "/game/common/coat_of_arms/coat_of_arms";
	auto filenames = commonItems::GetAllFilesInFolderRecursive(coasPath);
	LOG(LogLevel::Info) << "-> Parsing CoAs.";
	registerKeys();
	for (const auto& fileName : filenames)
	{
		parseFile(coasPath + "/" + fileName);
	}
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << coasMap.size() << " coats of arms.";
}

mappers::CoaMapper::CoaMapper(const std::string& coaFilePath)
{
	registerKeys();
	parseFile(coaFilePath);
	clearRegisteredKeywords();
}

void mappers::CoaMapper::registerKeys()
{
	registerKeyword("template", commonItems::ignoreItem); // we don't need templates, we need CoAs!
	registerRegex(commonItems::catchallRegex, [this](const std::string& flagName, std::istream& theStream) { // the rest should be CoAs
		auto coaItem = commonItems::stringOfItem(theStream).getString();
		coasMap.insert(std::pair(flagName, coaItem));
	});
}

std::optional<std::string> mappers::CoaMapper::getCoaForFlagName(const std::string& impFlagName)
{
	if (coasMap.contains(impFlagName)) return coasMap.at(impFlagName);
	return std::nullopt;
}