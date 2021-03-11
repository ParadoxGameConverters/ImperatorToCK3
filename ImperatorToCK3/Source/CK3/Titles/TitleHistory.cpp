#include "TitleHistory.h"
#include "CommonUtilities/HistoryFactory.h"
#include "ParserHelpers.h"



CK3::TitleHistory::TitleHistory(std::unique_ptr<History>& history) {
	const date date{867, 1, 1};

	holder = *history->getFieldValue("holder", date);
	liege = history->getFieldValue("liege", date);
	government = history->getFieldValue("government", date);
	developmentLevel = commonItems::stringToInteger<decltype(developmentLevel)>(*history->getFieldValue("development_level", date));
}
