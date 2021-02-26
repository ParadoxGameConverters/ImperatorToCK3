#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "HistoryFactory.h"


History::Factory::Factory(std::vector<SimpleFieldStruct> simpleFieldStructs): simpleFieldStructs(std::move(simpleFieldStructs))
{
	for (const auto& [fieldName, setter, _initialValue] : this->simpleFieldStructs)
	{
		setterFieldMap[setter] = fieldName;
		registerKeyword(setter, [this, &fieldName](std::istream& theStream) {
			history->simpleFields.at(fieldName).setInitialValue(commonItems::getString(theStream)); // if the value is set outside of dated blocks, override the initial value
		});
	}
	registerRegex(R"(\d+[.]\d+[.]\d+)", [&](const std::string& dateStr, std::istream& theStream)
	{
		const date date{ dateStr };
		const DatedHistoryEntry datedEntry{ theStream };
		for (const auto& [key, valuesVec] : datedEntry.getContents())
		{
			const auto& fieldName = setterFieldMap[key];
			if (history->simpleFields.contains(fieldName))
			{
				history->simpleFields[fieldName].addValueToHistory(valuesVec.back(), date);
			}
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<History> History::Factory::getHistory(std::istream& theStream)
{
	history = std::make_unique<History>();
	for (const auto& [fieldName, _setter, defaultValue] : simpleFieldStructs)
	{
		history->simpleFields[fieldName] = SimpleField{ defaultValue };
	}
	parseStream(theStream);
	return std::move(history);
}

