#ifndef DATED_HISTORY_BLOCK_H
#define DATED_HISTORY_BLOCK_H



#include "Parser.h"
#include <map>



struct DatedHistoryBlockReturnStruct {
	std::map<std::string, std::vector<std::string>> simpleFieldContents;
	std::map<std::string, std::vector<std::vector<std::string>>> containerFieldContents;
};

class DatedHistoryBlock: public commonItems::parser {
public:
	explicit DatedHistoryBlock(std::istream& theStream);
	[[nodiscard]] auto getContents() const { return contents; }
private:
	DatedHistoryBlockReturnStruct contents;
};


#endif // DATED_HISTORY_BLOCK_H