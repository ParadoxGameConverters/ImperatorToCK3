#include "History.h"

std::optional<std::string> History::getValue(const std::string& fieldName, const date& date) const {
	if (!fields.contains(fieldName))
		return std::nullopt;
	return fields[fieldName].getValue(date);
}

std::optional<std::string> FieldHistory::getValue(const date& date) {
	return std::optional<std::string>();
}
