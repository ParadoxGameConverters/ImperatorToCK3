#ifndef CK3_TITLE_H
#define CK3_TITLE_H



#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "TitleHistory.h"
#include "Imperator/Countries/CountryName.h"
#include "Parser.h"
#include "Color.h"
#include <memory>
#include <string>
#include <set>



extern commonItems::Color::Factory laFabricaDeColor;


namespace Imperator {
class Country;
}

namespace mappers {
class TagTitleMapper;
class CoaMapper;
class ProvinceMapper;
class GovernmentMapper;
class SuccessionLawMapper;
} // namespace mappers

namespace CK3 {

class LandedTitles;
class TitlesHistory;
enum class TitleRank { barony, county, duchy, kingdom, empire };
class Title: commonItems::parser, public std::enable_shared_from_this<Title>
{
public:
	Title() = default;
	explicit Title(const std::string& name);
	void initializeFromTag(
		std::shared_ptr<Imperator::Country> theCountry,
		const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries,
		mappers::LocalizationMapper& localizationMapper,
		LandedTitles& landedTitles,
		mappers::ProvinceMapper& provinceMapper,
		mappers::CoaMapper& coaMapper,
		mappers::TagTitleMapper& tagTitleMapper,
		mappers::GovernmentMapper& governmentMapper,
		mappers::SuccessionLawMapper& successionLawMapper
	);
	
	void updateFromTitle(const std::shared_ptr<Title>& otherTitle);
	void loadTitles(std::istream& theStream);

	void setHolder(const std::string& newHolder) { history.holder = newHolder; }
	void setDevelopmentLevel(const std::optional<int>& devLevel) { history.developmentLevel = devLevel; }
	void setLocalizations(const mappers::LocBlock& newBlock) { localizations[titleName] = newBlock; } // Setting the localized name
	void trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper, const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries);
	void addCountyProvince(const unsigned long long provinceId) { countyProvinces.emplace(provinceId); }
	void addHistory(const LandedTitles& landedTitles, TitleHistory titleHistory);
	
	void setDeJureLiege(const std::shared_ptr<Title>& liegeTitle);
	void setDeFactoLiege(const std::shared_ptr<Title>& liegeTitle);

	[[nodiscard]] const auto& getName() const { return titleName; }
	[[nodiscard]] auto getRank() const { return rank; }
	[[nodiscard]] const auto& getHolder() const { return history.holder; }
	[[nodiscard]] const auto& getGovernment() const { return history.government; }
	[[nodiscard]] const auto& getDevelopmentLevel() const { return history.developmentLevel; }
	[[nodiscard]] std::optional<int> getOwnOrInheritedDevelopmentLevel() const;
	[[nodiscard]] const auto& getSuccessionLaws() const { return successionLaws; }
	[[nodiscard]] auto isImportedOrUpdatedFromImperator() const { return importedOrUpdatedFromImperator; }

	[[nodiscard]] const auto& getDeJureLiege() const { return deJureLiege; }
	[[nodiscard]] const auto& getDeFactoLiege() const { return deFactoLiege; }
	
	[[nodiscard]] const auto& getDeJureVassals() const { return deJureVassals; }
	[[nodiscard]] const auto& getDeFactoVassals() const { return deFactoVassals; }
	[[nodiscard]] std::map<std::string, std::shared_ptr<Title>> getDeJureVassalsAndBelow(const std::string& rankFilter = "bcdke") const;
	[[nodiscard]] std::map<std::string, std::shared_ptr<Title>> getDeFactoVassalsAndBelow(const std::string& rankFilter = "bcdke") const;

	bool definiteForm = false;
	bool landless = false;
	std::map<std::string, mappers::LocBlock> localizations;
	std::optional<std::string> coa;
	std::optional<std::string> capitalCounty;
	std::shared_ptr<Imperator::Country> imperatorCountry;
	std::optional<commonItems::Color> color;
	
	std::pair<std::string, std::shared_ptr<Title>> capital;	// Capital county

	friend std::ostream& operator<<(std::ostream& output, const Title& title);

private:
	friend class LandedTitles;
	static void addFoundTitle(const std::shared_ptr<Title>& newTitle, std::map<std::string, std::shared_ptr<Title>>& foundTitles);
	
	void registerKeys();
	void setRank();

	std::string titleName; // e.g. d_latium
	TitleRank rank = TitleRank::duchy;
	std::set<std::string> successionLaws;

	bool importedOrUpdatedFromImperator = false;
	std::optional<commonItems::Color> color1;
	std::optional<commonItems::Color> color2;

	TitleHistory history;

	std::shared_ptr<Title> deJureLiege; // direct de jure liege title name, e.g. e_hispania
	std::shared_ptr<Title> deFactoLiege; // direct de facto liege title name, e.g. e_hispania

	std::map<std::string, std::shared_ptr<Title>> deJureVassals; // DIRECT de jure vassals
	std::map<std::string, std::shared_ptr<Title>> deFactoVassals; // DIRECT de facto vassals
	
	std::map<std::string, std::shared_ptr<Title>> foundTitles;			// title name, title. Titles are only held here during loading of landed_titles, then they are cleared


	// used by duchy titles only
public:
	[[nodiscard]] bool duchyContainsProvince(unsigned long long provinceID) const;


	// used by county titles only
public:
	[[nodiscard]] const auto& getCountyProvinces() const { return countyProvinces; }
	std::string capitalBarony; // used when parsing inside county to save first barony
	unsigned long long capitalBaronyProvince = 0;	// county barony's province; 0 is not a valid barony ID
private:
	std::set<unsigned long long> countyProvinces;


	// used by barony titles only
public:
	[[nodiscard]] const auto& getProvince() const { return province; }
private:
	std::optional<unsigned long long> province; // province is area on map. b_ barony is its corresponding title.
};

} // namespace CK3



#endif // CK3_TITLE_H
