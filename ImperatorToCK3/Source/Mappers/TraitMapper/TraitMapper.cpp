#include "TraitMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "TraitMapping.h"

mappers::TraitMapper::TraitMapper()
{
	LOG(LogLevel::Info) << "-> Parsing trait mappings.";
	registerKeys();
	parseFile("configurables/trait_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << impToCK3TraitMap.size() << " trait links.";
}

mappers::TraitMapper::TraitMapper(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::TraitMapper::registerKeys()
{
	registerKeyword("link", [this](const std::string& unused, std::istream& theStream) {
		const TraitMapping theMapping(theStream);
		for (const auto& imperatorTrait: theMapping.impTraits)
		{
			if (theMapping.ck3Trait) impToCK3TraitMap.insert(std::make_pair(imperatorTrait, *theMapping.ck3Trait));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> mappers::TraitMapper::getCK3TraitForImperatorTrait(const std::string& impTrait) const
{
	const auto& mapping = impToCK3TraitMap.find(impTrait);
	if (mapping != impToCK3TraitMap.end())
		return mapping->second;
	return std::nullopt;
}