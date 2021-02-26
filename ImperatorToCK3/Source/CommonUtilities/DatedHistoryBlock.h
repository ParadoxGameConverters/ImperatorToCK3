#ifndef DATED_HISTORY_BLOCK_H
#define DATED_HISTORY_BLOCK_H



#include "Parser.h"
#include <map>



class DatedHistoryBlock: public commonItems::parser {
public:
	explicit DatedHistoryBlock(std::istream& theStream);
	[[nodiscard]] auto getContents() const { return contents; }
private:
	std::map<std::string, std::vector<std::string>> contents;
};


#endif // DATED_HISTORY_BLOCK_H