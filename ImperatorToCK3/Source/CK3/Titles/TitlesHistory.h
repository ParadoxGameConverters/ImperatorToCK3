#ifndef CK3_TITLES_HISTORY_H
#define CK3_TITLES_HISTORY_H



#include "Configuration/Configuration.h"
#include "Parser.h"
#include "Date.h"
#include <map>
#include <optional>
#include <string>



namespace CK3
{
class DatedHistoryEntry : commonItems::parser
{
	/// <summary>
	/// This class stores the holder, liege and government from a single history entry (if there is one).
	/// Example entry: 856.1.1 = { holder = akan707 }
	/// What's saved in the class: holder = akan707
	/// </summary>
public:
	DatedHistoryEntry() = default;
	explicit DatedHistoryEntry(std::istream& theStream);

	std::optional<std::string> holder;
	std::optional<std::string> liege;
	std::optional<std::string> government;

private:
	void registerKeys();
}; // class DatedHistoryEntry

	
class TitleHistory : commonItems::parser
{
	/// <summary>
	/// This class stores the dated history entries for a single title
	/// </summary>
public:
	TitleHistory() = default;
	explicit TitleHistory(std::istream& theStream);

	std::pair<date, std::optional<std::string>> currentHolderWithDate = { date(1,1,1), std::nullopt}; // from entry with the closest date <= 867.1.1
	std::pair<date, std::optional<std::string>> currentLiegeWithDate = { date(1,1,1), std::nullopt}; // from entry with the closest date <= 867.1.1
	std::pair<date, std::optional<std::string>> currentGovernmentWithDate = { date(1,1,1), std::nullopt }; // from entry with the closest date <= 867.1.1
private:
	void registerKeys();
}; // class TitleHistory

	
class TitlesHistory : commonItems::parser
{
	/// <summary>
	/// This class stores vanilla titles history as strings. To save memory, title's history string is removed from the map before being returned.
	/// </summary>
public:
	TitlesHistory() = default;
	explicit TitlesHistory(const Configuration& theConfiguration);
	explicit TitlesHistory(const std::string& historyFilePath);

	[[nodiscard]] std::optional<std::string> popTitleHistory(const std::string& titleName); // "pop" as from stack, not Imperator Pop ;)
	std::map<std::string, std::optional<std::string>> currentHolderIdMap; // value is nullopt only when there is no holder registered before or at CK3 start date
	std::map<std::string, std::optional<std::string>> currentLiegeIdMap; // value is nullopt only when there is no liege registered before or at CK3 start date
	std::map<std::string, std::optional<std::string>> currentGovernmentMap; // value is nullopt only when there is no government registered before or at CK3 start date

private:
	void registerKeys();

	std::map<std::string, std::string> historyMap;
}; // class TitlesHistory
} // namespace CK3

#endif // CK3_TITLES_HISTORY_H
