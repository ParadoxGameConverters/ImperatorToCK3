#include "HistoryFactory.h"
#include "DatedHistoryBlock.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



History::Factory::Factory(std::vector<SimpleFieldStruct> _simpleFieldStructs, std::vector<ContainerFieldStruct> _containerFieldStructs):
	simpleFieldStructs(std::move(_simpleFieldStructs)), containerFieldStructs(std::move(_containerFieldStructs))
{
	for (const auto& [fieldName, setter, _initialValue] : simpleFieldStructs) {
		registerKeyword(setter, [&](std::istream& theStream) {
			// if the value is set outside of dated blocks, override the initial value
			history->simpleFields.at(fieldName).setInitialValue(commonItems::getString(theStream));
		});
	}
	for (const auto& [fieldName, setter, _initialValue] : containerFieldStructs) {
		registerKeyword(setter, [&](std::istream& theStream) {
			// if the value is set outside of dated blocks, override the initial value
			history->containerFields.at(fieldName).setInitialValue(commonItems::getStrings(theStream));
		});
	}
	registerRegex(R"(\d+[.]\d+[.]\d+)", [&](const std::string& dateStr, std::istream& theStream) {
		const date date{ dateStr };
		auto contents = DatedHistoryBlock{ simpleFieldStructs, containerFieldStructs, theStream }.getContents();
		for (const auto& [fieldName, valuesVec] : contents.simpleFieldContents) {
			history->simpleFields[fieldName].addValueToHistory(valuesVec.back(), date);
		}
		for (const auto& [fieldName, valuesVec] : contents.containerFieldContents) {
			history->containerFields[fieldName].addValueToHistory(valuesVec.back(), date);
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<History> History::Factory::getHistory(std::istream& theStream) {
	history = std::make_unique<History>();
	for (const auto& [fieldName, _setter, initialValue] : simpleFieldStructs) {
		history->simpleFields[fieldName] = SimpleField{ initialValue };
	}
	for (const auto& [fieldName, _setter, initialValue] : containerFieldStructs) {
		history->containerFields[fieldName] = ContainerField{ initialValue };
	}
	parseStream(theStream);
	return std::move(history);
}

