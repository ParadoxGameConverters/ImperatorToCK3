#include "ReligionMapper.h"
#include "Log.h"
#include "ParserHelpers.h"

mappers::ReligionMapper::ReligionMapper()
{
	LOG(LogLevel::Info) << "-> Parsing religion mappings.";
	registerKeys();
	parseFile("configurables/religion_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << religionMappings.size() << " religious links.";
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
		religionMappings.push_back(ReligionMapping{theStream});
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

void mappers::ReligionMapper::loadRegionMappers(const std::shared_ptr<mappers::ImperatorRegionMapper>& impRegionMapper, const std::shared_ptr<mappers::CK3RegionMapper>& ck3RegionMapper)
{
	for (auto& mapping : religionMappings)
	{
		mapping.insertImperatorRegionMapper(impRegionMapper);
		mapping.insertCK3RegionMapper(ck3RegionMapper);
	}
}

std::optional<std::string> mappers::ReligionMapper::match(const std::string& impReligion, const unsigned long long ck3ProvinceID, const unsigned long long impProvinceID) const
{
	for (const auto& religionMapping : religionMappings)
	{
		const auto& possibleMatch = religionMapping.religionMatch(impReligion, ck3ProvinceID, impProvinceID);
		if (possibleMatch)
			return *possibleMatch;
	}
	return std::nullopt;
}