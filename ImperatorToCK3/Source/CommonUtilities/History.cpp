#include "ParserHelpers.h"
#include "History.h"



std::optional<std::string> History::getFieldValue(const std::string& fieldName, const date& date) const {
	const auto itr = simpleFields.find(fieldName);
	if (itr == simpleFields.end()) {
		return std::nullopt;
	}

	return itr->second.getValue(date);
}
