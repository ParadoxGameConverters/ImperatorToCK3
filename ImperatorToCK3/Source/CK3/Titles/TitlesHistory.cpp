#include "TitlesHistory.h"
#include "CommonUtilities/HistoryFactory.h"
#include "Configuration/Configuration.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "OSCompatibilityLayer.h"




CK3::TitlesHistory::TitlesHistory(const Configuration& theConfiguration) {
	const auto historyPath = theConfiguration.getCK3Path() + "/game/history/titles";
	auto filenames = commonItems::GetAllFilesInFolderRecursive(historyPath);
	LOG(LogLevel::Info) << "-> Parsing title history.";
	registerKeys();
	for (const auto& fileName : filenames) {
		parseFile(historyPath + "/" + fileName);
	}
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << historyMap.size() << " title histories.";
}

std::optional<CK3::TitleHistory> CK3::TitlesHistory::popTitleHistory(const std::string& titleName) {
	if (auto historyItr = historyMap.find(titleName); historyItr != historyMap.end()) {
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
			auto history = historyFactory.getHistory(tempStream);
			historyMap.emplace(titleName, TitleHistory(history));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
