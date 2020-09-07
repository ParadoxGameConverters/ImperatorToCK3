#include "CultureMapper.h"
#include "Log.h"
#include "ParserHelpers.h"

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
	registerKeyword("link", [this](const std::string& unused, std::istream& theStream) {
		const CultureMappingRule rule(theStream);
		cultureMapRules.push_back(rule);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> mappers::CultureMapper::cultureMatch(const std::string& impCulture,
	 const std::string& ck3religion,
	 int ck3Province,
	 const std::string& ck3ownerTag) const
{
	for (const auto& cultureMappingRule: cultureMapRules)
	{
		const auto& possibleMatch = cultureMappingRule.cultureMatch(impCulture, ck3religion, ck3Province, ck3ownerTag);
		if (possibleMatch)
			return *possibleMatch;
	}
	return std::nullopt;
}

std::optional<std::string> mappers::CultureMapper::cultureNonReligiousMatch(const std::string& impCulture,
	const std::string& ck3religion,
	int ck3Province,
	const std::string& ck3ownerTag) const
{
	for (const auto& cultureMappingRule : cultureMapRules)
	{
		const auto& possibleMatch = cultureMappingRule.cultureNonReligiousMatch(impCulture, ck3religion, ck3Province, ck3ownerTag);
		if (possibleMatch)
			return *possibleMatch;
	}
	return std::nullopt;
}