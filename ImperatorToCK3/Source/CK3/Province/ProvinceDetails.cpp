#include "ProvinceDetails.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "CommonRegexes.h"



History::Factory CK3::ProvinceDetails::historyFactory = History::Factory({
	{"culture", "culture", std::nullopt},
	{"religion", "religion", std::nullopt},
	{"holding", "holding", "none"}
});

CK3::ProvinceDetails::ProvinceDetails(std::istream& theStream)
{
	const auto history = historyFactory.getHistory(theStream);
	const date date{867, 1, 1};
	const auto cultureOpt = history->getFieldValue("culture", date);
	const auto religionOpt = history->getFieldValue("religion", date);
	const auto holdingOpt = history->getFieldValue("holding", date);
	if (cultureOpt) {
		culture = *cultureOpt;
	}
	if (religionOpt) {
		religion = *religionOpt;
	}
	if (holdingOpt) {
		holding = *holdingOpt;
	}
}
