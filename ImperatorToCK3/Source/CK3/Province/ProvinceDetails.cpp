#include "ProvinceDetails.h"
#include "ParserHelpers.h"



History::Factory CK3::ProvinceDetails::historyFactory = History::Factory(
	{ // simple fields
		{ .fieldName="culture", .setter="culture", .initialValue=std::nullopt },
		{ .fieldName="religion", .setter="religion", .initialValue=std::nullopt },
		{ .fieldName="holding", .setter="holding", .initialValue="none" },
	},
	{} // container fields
);


CK3::ProvinceDetails::ProvinceDetails(std::istream& theStream) {
	const auto history = historyFactory.getHistory(theStream);
	const date date{867, 1, 1};
	
	if (const auto cultureOpt = history->getSimpleFieldValue("culture", date)) {
		culture = *cultureOpt;
	}
	if (const auto religionOpt = history->getSimpleFieldValue("religion", date)) {
		religion = *religionOpt;
	}
	holding = *history->getSimpleFieldValue("holding", date);
}
