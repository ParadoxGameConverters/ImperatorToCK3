#ifndef SIMPLE_FIELD_H
#define SIMPLE_FIELD_H



#include "Parser.h"
#include "Date.h"
#include <map>



struct SimpleFieldStruct {
	std::string fieldName;
	std::string setter;
	std::optional<std::string> initialValue;
};

class SimpleField {
public:
	SimpleField() = default;
	explicit SimpleField(std::optional<std::string> initialValue);
	[[nodiscard]] std::optional<std::string> getValue(const date& date) const;
	[[nodiscard]] const auto& getValueHistory() const { return valueHistory; }

	void setInitialValue(const std::optional<std::string>& newValue) { initialValue = newValue; }
	void addValueToHistory(const std::string& value, const date& date) { valueHistory[date] = value; }

private:
	std::map<date, std::string, std::greater<>> valueHistory;
	std::optional<std::string> initialValue;
};

#endif // SIMPLE_FIELD_H