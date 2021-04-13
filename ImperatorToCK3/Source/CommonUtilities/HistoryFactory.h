#ifndef HISTORY_FACTORY_H
#define HISTORY_FACTORY_H



#include "History.h"
#include "Parser.h"
#include <memory>



class History::Factory: commonItems::parser {
public:
	explicit Factory(std::vector<SimpleFieldStruct> _simpleFieldStructs, std::vector<ContainerFieldStruct> _containerFieldStructs);
	std::unique_ptr<History> getHistory(std::istream& theStream);

private:
	std::unique_ptr<History> history;
	std::vector<SimpleFieldStruct> simpleFieldStructs; // fieldName, setter, initialValue
	std::vector<ContainerFieldStruct> containerFieldStructs; // fieldName, setter, initialValue
};



#endif // HISTORY_FACTORY_H