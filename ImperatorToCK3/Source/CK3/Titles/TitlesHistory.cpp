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
	});
	registerKeyword(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> CK3::TitlesHistory::popTitleHistory(const std::string& titleName)
{
	if (historyMap.find(titleName) != historyMap.end())
	{
		auto history = historyMap[titleName];
		historyMap.erase("titleName");
		return history;
	}
	return std::nullopt;
}