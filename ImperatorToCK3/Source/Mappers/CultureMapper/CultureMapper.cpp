#include "CultureMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::CultureMapper::CultureMapper(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

mappers::CultureMapper::CultureMapper()
{
	LOG(LogLevel::Info) << "-> Parsing culture mappings.";
	registerKeys();
	parseFile("configurables/culture_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << cultureMapRules.size() << " cultural links.";
}

void mappers::CultureMapper::registerKeys()
{
	registerKeyword("link", [this](std::istream& theStream) {
		const CultureMappingRule rule(theStream);
		cultureMapRules.push_back(rule);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> mappers::CultureMapper::match(const std::string& impCulture,
	 const std::string& ck3religion,
	 const unsigned long long ck3ProvinceID,
	 const unsigned long long impProvinceID,
	 const std::string& ck3ownerTitle) const
{
	for (const auto& cultureMappingRule: cultureMapRules)
	{
		const auto& possibleMatch = cultureMappingRule.match(impCulture, ck3religion, ck3ProvinceID, impProvinceID, ck3ownerTitle);
		if (possibleMatch)
			return *possibleMatch;
	}
	return std::nullopt;
}

std::optional<std::string> mappers::CultureMapper::nonReligiousMatch(const std::string& impCulture,
	const std::string& ck3religion,
	const unsigned long long ck3ProvinceID,
	const unsigned long long impProvinceID,
	const std::string& ck3ownerTitle) const
{
	for (const auto& cultureMappingRule : cultureMapRules)
	{
		const auto& possibleMatch = cultureMappingRule.nonReligiousMatch(impCulture, ck3religion, ck3ProvinceID, impProvinceID, ck3ownerTitle);
		if (possibleMatch)
			return *possibleMatch;
	}
	return std::nullopt;
}

void mappers::CultureMapper::loadRegionMappers(std::shared_ptr<ImperatorRegionMapper> impRegionMapper, std::shared_ptr<CK3RegionMapper> _ck3RegionMapper)
{
	const auto imperatorRegionMapper = std::move(impRegionMapper);
	const auto ck3RegionMapper = std::move(_ck3RegionMapper);
	if (!imperatorRegionMapper)
		throw std::runtime_error("Culture Mapper: Imperator Region Mapper is unloaded!");
	if (!ck3RegionMapper)
		throw std::runtime_error("Culture Mapper: CK3 Region Mapper is unloaded!");
	for (auto& mapping : cultureMapRules)
	{
		mapping.insertImperatorRegionMapper(imperatorRegionMapper);
		mapping.insertCK3RegionMapper(ck3RegionMapper);
	}
}
