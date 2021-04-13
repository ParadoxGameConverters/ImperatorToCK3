#include "ContainerField.h"



ContainerField::ContainerField(std::vector<std::string> initialValue): initialValue(std::move(initialValue)) {}


std::vector<std::string> ContainerField::getValue(const date& date) const {
	if (const auto& lowerBound = valueHistory.lower_bound(date); lowerBound != valueHistory.end()) {
		return lowerBound->second;
	}
	return {};
}
