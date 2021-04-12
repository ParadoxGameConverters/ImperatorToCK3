#include "History.h"
#include "ParserHelpers.h"



std::optional<std::string> History::getSimpleFieldValue(const std::string& fieldName, const date& date) const {
	const auto itr = simpleFields.find(fieldName);
	if (itr == simpleFields.end()) {
		return std::nullopt;
	}

	return itr->second.getValue(date);
}
