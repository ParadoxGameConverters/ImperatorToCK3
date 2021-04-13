#ifndef CONTAINER_FIELD_H
#define CONTAINER_FIELD_H



#include "Date.h"
#include <map>
#include <vector>



struct ContainerFieldStruct {
	std::string fieldName;
	std::string setter;
	std::vector<std::string> initialValue;
};

class ContainerField {
public:
	ContainerField() = default;
	explicit ContainerField(std::vector<std::string> initialValue);
	[[nodiscard]] std::vector<std::string> getValue(const date& date) const;
	[[nodiscard]] const auto& getValueHistory() const { return valueHistory; }

	void setInitialValue(std::vector<std::string> newValue) { initialValue = std::move(newValue); }
	void addValueToHistory(const std::vector<std::string>& value, const date& date) { valueHistory[date] = value; }

private:
	std::map<date, std::vector<std::string>, std::greater<>> valueHistory;
	std::vector<std::string> initialValue;
};



#endif // CONTAINER_FIELD_H