#include "DatedHistoryBlock.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



DatedHistoryBlock::DatedHistoryBlock(std::vector<SimpleFieldStruct> _simpleFieldStructs, std::vector<ContainerFieldStruct> _containerFieldStructs, std::istream& theStream):
	simpleFieldStructs(std::move(_simpleFieldStructs)), containerFieldStructs(std::move(_containerFieldStructs))
{
	for (const auto& [fieldName, setter, _initialValue] : simpleFieldStructs) {
		registerKeyword(setter, [&](std::istream& theStream) {
			contents.simpleFieldContents[fieldName].emplace_back(commonItems::getString(theStream));
		});
	}
	for (const auto& [fieldName, setter, _initialValue] : containerFieldStructs) {
		registerKeyword(setter, [&](std::istream& theStream) {
			contents.containerFieldContents[fieldName].emplace_back(commonItems::getStrings(theStream));
		});
	}
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
	parseStream(theStream);
	clearRegisteredKeywords();
}
