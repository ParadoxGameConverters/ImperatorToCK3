#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "History.h"


SimpleField::SimpleField(const std::optional<std::string>& initialValue) : initialValue(initialValue) {}

std::optional<std::string> SimpleField::getValue(const date& date) const {
	if (const auto& lowerBound = valueHistory.lower_bound(date); lowerBound != valueHistory.end()) {
		return lowerBound->second;
	}
	return initialValue;
}



DatedHistoryEntry::DatedHistoryEntry(std::istream& theStream)
{
	registerRegex(commonItems::stringRegex, [&](const std::string& key, std::istream& theStream) {
		auto rightSide = commonItems::getString(theStream);
		if (contents.contains(key)) [[unlikely]]
			contents[key].emplace_back(rightSide);
		else [[likely]]
			contents[key] = { rightSide };
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
	parseStream(theStream);
	clearRegisteredKeywords();
}



std::optional<std::string> History::getFieldValue(const std::string& fieldName, const date& date) const {
	if (!simpleFields.contains(fieldName))
		return std::nullopt;
	return simpleFields.at(fieldName).getValue(date);
}
