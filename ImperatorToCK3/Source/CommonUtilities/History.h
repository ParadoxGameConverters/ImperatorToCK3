#ifndef HISTORY_H
#define HISTORY_H



#include "SimpleField.h"
#include "Parser.h"
#include "Date.h"
#include <map>



class History {
public:
	History() = default;
	class Factory;
	[[nodiscard]] std::optional<std::string> getSimpleFieldValue(const std::string& fieldName, const date& date) const; // for non-container fields
	[[nodiscard]] std::optional<std::vector<std::string>> getContainerFieldValue(const std::string& fieldName, const date& date) const; // for container fields
private:
	std::map<std::string, SimpleField> simpleFields; // fieldName, field
};



#endif // HISTORY_H