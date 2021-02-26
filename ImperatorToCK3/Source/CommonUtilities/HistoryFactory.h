#ifndef HISTORY_FACTORY_H
#define HISTORY_FACTORY_H



#include "Parser.h"
#include "History.h"
#include <memory>





class History::Factory: commonItems::parser
{
  public:
	explicit Factory(std::vector<SimpleFieldStruct> simpleFieldStructs);
	std::unique_ptr<History> getHistory(std::istream& theStream);

  private:
	std::unique_ptr<History> history;
	std::vector<SimpleFieldStruct> simpleFieldStructs; // fieldName, setter, defaultValue
	std::map<std::string, std::string> setterFieldMap; // setter, fieldName
};


#endif // HISTORY_FACTORY_H