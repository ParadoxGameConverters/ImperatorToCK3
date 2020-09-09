#ifndef LOCALIZATION_MAPPER
#define LOCALIZATION_MAPPER
#include <map>
#include <optional>
#include <string>

class Configuration;
namespace mappers
{
enum class langEnum
{
	ENGLISH,
	FRENCH,
	GERMAN,
	RUSSIAN,
	SPANISH
};
	
class LocalizationMapper
{
  public:
	LocalizationMapper() = default;
	void scrapeLocalizations(const Configuration& theConfiguration);
	void scrapeStream(std::istream& theStream, langEnum language);

	[[nodiscard]] std::optional<std::string> getLocBlockForKey(const std::string& key, langEnum language) const;

  private:
	std::map<std::string, std::string> localizationsEnglish;
	std::map<std::string, std::string> localizationsFrench;
	std::map<std::string, std::string> localizationsGerman;
	std::map<std::string, std::string> localizationsRussian;
	std::map<std::string, std::string> localizationsSpanish;
};
} // namespace mappers

#endif // LOCALIZATION_MAPPER