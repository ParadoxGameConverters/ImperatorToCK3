#ifndef DATED_HISTORY_BLOCK_H
#define DATED_HISTORY_BLOCK_H



#include "SimpleField.h"
#include "ContainerField.h"
#include "Parser.h"
#include <map>



struct DatedHistoryBlockReturnStruct {
	std::map<std::string, std::vector<std::string>> simpleFieldContents;
	std::map<std::string, std::vector<std::vector<std::string>>> containerFieldContents;
};

class DatedHistoryBlock: public commonItems::parser {
public:
	explicit DatedHistoryBlock(std::vector<SimpleFieldStruct> _simpleFieldStructs, std::vector<ContainerFieldStruct> _containerFieldStructs, std::istream& theStream);
	[[nodiscard]] auto getContents() const { return contents; }
private:
	DatedHistoryBlockReturnStruct contents;
	std::vector<SimpleFieldStruct> simpleFieldStructs; // fieldName, setter, defaultValue
	std::vector<ContainerFieldStruct> containerFieldStructs; // fieldName, setter
};


#endif // DATED_HISTORY_BLOCK_H