#include "SimpleField.h"



SimpleField::SimpleField(std::optional<std::string> initialValue) : initialValue(std::move(initialValue)) {}

std::optional<std::string> SimpleField::getValue(const date& date) const {
	if (const auto& lowerBound = valueHistory.lower_bound(date); lowerBound != valueHistory.end()) {
		return lowerBound->second;
	}
	return initialValue;
}
