#ifndef CK3_TITLES_HISTORY_H
#define CK3_TITLES_HISTORY_H



#include "Parser.h"
#include "TitleHistory.h"



class Configuration;

namespace CK3 {
	
class TitlesHistory : commonItems::parser {
	/// <summary>
	/// This class stores vanilla titles history. To save memory, title's history is removed from the map before being returned.
	/// </summary>
public:
	TitlesHistory() = default;
	explicit TitlesHistory(const std::string& folderPath);
	[[nodiscard]] std::optional<TitleHistory> popTitleHistory(const std::string& titleName); // "pop" as from stack, not Imperator Pop ;)
private:
	void registerKeys();
	History::Factory historyFactory= History::Factory({
		{ .fieldName="holder", .setter="holder", .initialValue="0" },
		{ .fieldName="liege", .setter="liege", .initialValue=std::nullopt },
		{ .fieldName="government", .setter="government", .initialValue=std::nullopt },
		{ .fieldName="development_level", .setter="change_development_level", .initialValue=std::nullopt }
	});
	std::map<std::string, TitleHistory> historyMap;
}; // class TitlesHistory

} // namespace CK3



#endif // CK3_TITLES_HISTORY_H
