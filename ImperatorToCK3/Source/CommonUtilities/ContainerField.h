#ifndef CONTAINER_FIELD_H
#define CONTAINER_FIELD_H



#include "Date.h"
#include <map>
#include <vector>



struct ContainerFieldStruct {
	std::string fieldName;
	std::string setter;
};

class ContainerField {
public:
	ContainerField() = default;
	[[nodiscard]] std::vector<std::string> getValue(const date& date) const;
	[[nodiscard]] const auto& getValueHistory() const { return valueHistory; }

	void addValueToHistory(const std::vector<std::string>& value, const date& date) { valueHistory[date] = value; }

private:
	std::map<date, std::vector<std::string>, std::greater<>> valueHistory;
};



#endif // CONTAINER_FIELD_H