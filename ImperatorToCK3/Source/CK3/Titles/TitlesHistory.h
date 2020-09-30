#ifndef CK3_TITLES_HISTORY_H
#define CK3_TITLES_HISTORY_H

#include "Parser.h"
#include <map>
#include <optional>
#include <string>
#include "../../Configuration/Configuration.h"

namespace CK3
{
class TitlesHistory: commonItems::parser
{
  /// <summary>
  /// This class stores vanilla titles history as strings. To save memory, title's history is removed from the map before being returned.
  /// </summary>
  public:
	TitlesHistory() = default;
	explicit TitlesHistory(const Configuration& theConfiguration);
	explicit TitlesHistory(const std::string& historyFilePath);

	[[nodiscard]] std::optional<std::string> popTitleHistory(const std::string& titleName);

  private:
	void registerKeys();

	std::map<std::string, std::string> historyMap;
};
} // namespace CK3

#endif // CK3_TITLES_HISTORY_H
