#ifndef CK3_TITLE_H
#define CK3_TITLE_H



#include "TitleHistory.h"
#include "Imperator/Countries/CountryName.h"
#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Color.h"
#include "Parser.h"
#include <memory>
#include <set>
#include <string>



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
class Character;

enum class TitleRank { barony, county, duchy, kingdom, empire };

class Title: commonItems::parser, public std::enable_shared_from_this<Title> {
public:
	Title() = default;
	explicit Title(std::string name);
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

	void setHolder(const std::shared_ptr<Character>& newPtr);
	void setDevelopmentLevel(const std::optional<int>& devLevel) { history.developmentLevel = devLevel; }
	void setLocalizations(const mappers::LocBlock& newBlock) { localizations[titleName] = newBlock; } // Setting the localized name
	void trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper, const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries);
	void addCountyProvince(const unsigned long long provinceId) { countyProvinces.emplace(provinceId); }
	void addHistory(const LandedTitles& landedTitles, TitleHistory titleHistory);

	[[nodiscard]] const auto& getLocalizations() const { return localizations; }
	[[nodiscard]] const auto& getCoA() const { return coa; }
	[[nodiscard]] const auto& getCapitalCounty() const { return capitalCounty; }
	[[nodiscard]] const auto& getImperatorCountry() const { return imperatorCountry; }
	[[nodiscard]] const auto& getColor() const { return color; }
	
	void setDeJureLiege(const std::shared_ptr<Title>& liegeTitle);
	void setDeFactoLiege(const std::shared_ptr<Title>& liegeTitle);

	[[nodiscard]] const auto& getName() const { return titleName; }
	[[nodiscard]] auto getRank() const { return rank; }
	[[nodiscard]] auto isLandless() const { return landless; }
	[[nodiscard]] auto hasDefiniteForm() const { return definiteForm; }
	[[nodiscard]] const auto& getHolderID() const { return history.holder; }
	[[nodiscard]] const auto& getHolderPtr() const { return holderPtr; }
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

	[[nodiscard]] auto kingdomContainsProvince(unsigned long long provinceID) const;

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
	bool definiteForm = false;
	bool landless = false;
	std::optional<commonItems::Color> color1;
	std::optional<commonItems::Color> color2;

	std::map<std::string, mappers::LocBlock> localizations;
	std::optional<std::string> coa;
	std::optional<std::pair<std::string, std::shared_ptr<Title>>> capitalCounty;	// Capital county
	std::shared_ptr<Imperator::Country> imperatorCountry;
	std::optional<commonItems::Color> color;

	TitleHistory history;
	std::shared_ptr<Character> holderPtr = nullptr;

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
