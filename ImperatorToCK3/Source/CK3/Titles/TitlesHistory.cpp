#include "TitlesHistory.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "OSCompatibilityLayer.h"

CK3::TitlesHistory::TitlesHistory(const Configuration& theConfiguration)
{
	const auto historyPath = theConfiguration.getCK3Path() + "/game/history/titles";
	auto filenames = commonItems::GetAllFilesInFolderRecursive(historyPath);
	LOG(LogLevel::Info) << "-> Parsing title history.";
	registerKeys();
	for (const auto& fileName : filenames)
	{
		parseFile(historyPath + "/" + fileName);
	}
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << historyMap.size() << " title histories.";
}

CK3::TitlesHistory::TitlesHistory(const std::string& historyFilePath)
{
	registerKeys();
	parseFile(historyFilePath);
	clearRegisteredKeywords();
}

void CK3::TitlesHistory::TitlesHistory::registerKeys()
{
	registerRegex(R"((e|k|d|c|b)_[A-Za-z0-9_\-\']+)", [this](const std::string& titleName, std::istream& theStream) {
		const auto historyItem = commonItems::stringOfItem(theStream).getString();
		historyMap.insert(std::pair(titleName, historyItem.substr(3, historyItem.size()-4))); // inserts without the opening and closing bracket

		std::stringstream tempStream(historyItem);
		if (historyItem.find('{') != std::string::npos)
		{
			const auto titleHistory = TitleHistory(tempStream);
			currentHolderIdMap[titleName] = titleHistory.currentHolderWithDate.second;
			currentLiegeIdMap[titleName] = titleHistory.currentLiegeWithDate.second;
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> CK3::TitlesHistory::popTitleHistory(const std::string& titleName)
{
	if (historyMap.contains(titleName))
	{
		auto history = historyMap.at(titleName);
		historyMap.erase("titleName");
		return history;
	}
	return std::nullopt;
}



CK3::TitleHistory::TitleHistory(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}
void CK3::TitleHistory::TitleHistory::registerKeys()
{
	registerRegex(R"(\d+[.]\d+[.]\d+)", [this](const std::string& dateStr, std::istream& theStream) {
		auto historyEntry = DatedHistoryEntry(theStream);
		if (date(dateStr) >= currentHolderWithDate.first && date(dateStr) <= date(867, 1, 1) && historyEntry.holder)
			currentHolderWithDate = std::pair(date(dateStr), *historyEntry.holder);
		if (date(dateStr) >= currentLiegeWithDate.first && date(dateStr) <= date(867, 1, 1) && historyEntry.liege)
			currentLiegeWithDate = std::pair(date(dateStr), *historyEntry.liege);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}



CK3::DatedHistoryEntry::DatedHistoryEntry(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}
void CK3::DatedHistoryEntry::DatedHistoryEntry::registerKeys()
{
	registerKeyword("holder", [this](const std::string& unused, std::istream& theStream) {
		holder = commonItems::singleString(theStream).getString();
	});
	registerKeyword("liege", [this](const std::string& unused, std::istream& theStream) {
		liege = commonItems::singleString(theStream).getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}