#include "History.h"
#include "ParserHelpers.h"



std::optional<std::string> History::getSimpleFieldValue(const std::string& fieldName, const date& date) const {
	const auto itr = simpleFields.find(fieldName);
	if (itr == simpleFields.end()) {
		return std::nullopt;
	}

	return itr->second.getValue(date);
}


std::optional<std::vector<std::string>> History::getContainerFieldValue(const std::string& fieldName, const date& date) const {
	const auto itr = containerFields.find(fieldName);
	if (itr == containerFields.end()) {
		return std::nullopt;
	}

	return itr->second.getValue(date);
}
