#ifndef HISTORY_H
#define HISTORY_H


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
	std::optional<std::string> initialValue = std::nullopt;
};



class DatedHistoryEntry: public commonItems::parser {
public:
	explicit DatedHistoryEntry(std::istream& theStream);
	[[nodiscard]] const auto& getContents() const { return contents; }
private:
	std::map<std::string, std::vector<std::string>> contents;
};



class History {
public:
	History() = default;
	class Factory;
	[[nodiscard]] std::optional<std::string> getFieldValue(const std::string& fieldName, const date& date) const; // for non-container fields
private:
	std::map<std::string, SimpleField> simpleFields; // fieldName, field
};

#endif // HISTORY_H