#ifndef HISTORY_H
#define HISTORY_H



#include "Parser.h"
#include "Date.h"
#include <map>
#include "SimpleField.h"



class History {
public:
	History() = default;
	class Factory;
	[[nodiscard]] std::optional<std::string> getFieldValue(const std::string& fieldName, const date& date) const; // for non-container fields
private:
	std::map<std::string, SimpleField> simpleFields; // fieldName, field
};

#endif // HISTORY_H