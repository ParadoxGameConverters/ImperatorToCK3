#include "TagTitleMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "Imperator/Countries/Country.h"


mappers::TagTitleMapper::TagTitleMapper()
{
	LOG(LogLevel::Info) << "-> Parsing Title mappings";
	registerKeys();
	parseFile("configurables/title_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> " << theMappings.size() << " title mappings loaded.";
}

void mappers::TagTitleMapper::registerKeys()
{
	registerKeyword("link", [this](std::istream& theStream) {
		theMappings.emplace_back(TagTitleMapping(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}

std::string mappers::TagTitleMapper::getCK3TitleRank(Imperator::countryRankEnum impRank, const std::string& localizedTitleName) const
{
	if (localizedTitleName.find("Empire") != std::string::npos) return "e";
	else if (localizedTitleName.find("Kingdom") != std::string::npos) return "k";
	else switch (impRank)
	{
	case Imperator::countryRankEnum::migrantHorde: return "d";
	case Imperator::countryRankEnum::cityState: return "d";
	case Imperator::countryRankEnum::localPower: return "k";
	case Imperator::countryRankEnum::regionalPower: return "k";
	case Imperator::countryRankEnum::majorPower: return "k";
	case Imperator::countryRankEnum::greatPower: return "e";
	}
}

void mappers::TagTitleMapper::registerTag(const std::string& impTag, const std::string& ck3Title)
{
	registeredTagTitles.emplace(impTag, ck3Title);
	usedTitles.insert(ck3Title);
}


std::optional<std::string> mappers::TagTitleMapper::getTitleForTag(const std::string& impTag, const Imperator::countryRankEnum countryRank, const std::string& localizedTitleName)
{
	// the only case where we fail is on invalid invocation. Otherwise, failure is
	// not an option!
	if (impTag.empty())
		return std::nullopt;

	// look up register
	if (const auto& registerItr = registeredTagTitles.find(impTag); registerItr != registeredTagTitles.end())
		return registerItr->second;

	// Attempt a title match
	for (const auto& mapping : theMappings)
	{
		const auto& match = mapping.tagRankMatch(impTag, getCK3TitleRank(countryRank, localizedTitleName));
		if (match)
		{
			if (usedTitles.contains(*match))
				continue;
			registerTag(impTag, *match);
			return *match;
		}
	}

	// Generate a new tag
	auto generatedTitle = generateNewTitle(impTag, countryRank, localizedTitleName);
	registerTag(impTag, generatedTitle);
	return generatedTitle;
}

std::string mappers::TagTitleMapper::generateNewTitle(const std::string& impTag, const Imperator::countryRankEnum countryRank, const std::string& localizedTitleName) const
{
	std::string ck3Tag = getCK3TitleRank(countryRank, localizedTitleName);
	ck3Tag += "_";
	ck3Tag += generatedCK3TitlePrefix;
	ck3Tag += impTag;
	
	return ck3Tag;
}
