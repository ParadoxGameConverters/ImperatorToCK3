#include "ReligionMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "ReligionMapping.h"

mappers::ReligionMapper::ReligionMapper()
{
	LOG(LogLevel::Info) << "-> Parsing religion mappings.";
	registerKeys();
	parseFile("configurables/religion_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << impToCK3ReligionMap.size() << " religious links.";
}

mappers::ReligionMapper::ReligionMapper(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ReligionMapper::registerKeys()
{
	registerKeyword("link", [this](const std::string& unused, std::istream& theStream) {
		const ReligionMapping theMapping(theStream);
		for (const auto& imperatorReligion: theMapping.getImperatorReligions())
		{
			impToCK3ReligionMap.insert(std::make_pair(imperatorReligion, theMapping.getCK3Religion()));
		}
	});
	registerRegex("[a-zA-Z0-9\\_.:]+", commonItems::ignoreItem);
}

std::optional<std::string> mappers::ReligionMapper::getCK3ReligionForImperatorReligion(const std::string& impReligion) const
{
	const auto& mapping = impToCK3ReligionMap.find(impReligion);
	if (mapping != impToCK3ReligionMap.end())
		return mapping->second;
	return std::nullopt;
}