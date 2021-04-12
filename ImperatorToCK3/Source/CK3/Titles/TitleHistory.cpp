#include "TitleHistory.h"
#include "CommonUtilities/HistoryFactory.h"
#include "ParserHelpers.h"



CK3::TitleHistory::TitleHistory(const std::unique_ptr<History>& history) {
	const date date{867, 1, 1};

	holder = *history->getSimpleFieldValue("holder", date);
	liege = history->getSimpleFieldValue("liege", date);
	government = history->getSimpleFieldValue("government", date);

	const auto developmentLevelOpt = history->getSimpleFieldValue("development_level", date);
	if (developmentLevelOpt) {
		developmentLevel = commonItems::stringToInteger<int>(*developmentLevelOpt);
	}
}
