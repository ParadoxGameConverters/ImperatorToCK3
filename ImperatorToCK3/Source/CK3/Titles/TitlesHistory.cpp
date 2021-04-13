#include "TitlesHistory.h"
#include "CommonUtilities/HistoryFactory.h"
#include "Configuration/Configuration.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "OSCompatibilityLayer.h"



CK3::TitlesHistory::TitlesHistory(const std::string& folderPath) {
	auto filenames = commonItems::GetAllFilesInFolderRecursive(folderPath);
	LOG(LogLevel::Info) << "-> Parsing title history.";
	registerKeys();
	for (const auto& fileName : filenames) {
		parseFile(folderPath + "/" + fileName);
	}
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << historyMap.size() << " title histories.";
}

std::optional<CK3::TitleHistory> CK3::TitlesHistory::popTitleHistory(const std::string& titleName) {
	if (const auto historyItr = historyMap.find(titleName); historyItr != historyMap.end()) {
		auto history = historyItr->second;
		historyMap.erase(titleName);
		return history;
	}
	return std::nullopt;
}


void CK3::TitlesHistory::TitlesHistory::registerKeys() {
	registerRegex(R"((e|k|d|c|b)_[A-Za-z0-9_\-\']+)", [this](const std::string& titleName, std::istream& theStream) {
		const auto historyItem = commonItems::stringOfItem(theStream).getString();
		std::stringstream tempStream(historyItem);
		if (historyItem.find('{') != std::string::npos) {
			const auto history = historyFactory.getHistory(tempStream);
			historyMap.emplace(titleName, TitleHistory(history));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
