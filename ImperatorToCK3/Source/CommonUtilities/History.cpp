#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "History.h"



SimpleField::SimpleField(std::optional<std::string> initialValue) : initialValue(std::move(initialValue)) {}

std::optional<std::string> SimpleField::getValue(const date& date) const {
	if (const auto& lowerBound = valueHistory.lower_bound(date); lowerBound != valueHistory.end()) {
		return lowerBound->second;
	}
	return initialValue;
}



DatedHistoryEntry::DatedHistoryEntry(std::istream& theStream)
{
	registerRegex(commonItems::stringRegex, [&](const std::string& key, std::istream& theStream) {
		contents[key].emplace_back(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
	parseStream(theStream);
	clearRegisteredKeywords();
}



std::optional<std::string> History::getFieldValue(const std::string& fieldName, const date& date) const {
	const auto itr = simpleFields.find(fieldName);
	if (itr == simpleFields.end())
	{
		return std::nullopt;
	}

	return itr->second.getValue(date);
}
