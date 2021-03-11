#ifndef CK3_TITLE_HISTORY_H
#define CK3_TITLE_HISTORY_H



#include "CommonUtilities/HistoryFactory.h"



namespace CK3 {

struct TitleHistory {
	TitleHistory() = default;
	explicit TitleHistory(std::unique_ptr<History>& history);

	// These values are open to ease management.
	// This is a storage container for CK3::Title.
	std::string holder = "0"; // ID of Character holding the Title
	std::optional<std::string> liege;
	std::optional<std::string> government;
	std::optional<int> developmentLevel = 0;
};

} // namespace CK3



#endif // CK3_TITLE_HISTORY_H
