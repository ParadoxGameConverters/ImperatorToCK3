#ifndef CK3_TITLE_H
#define CK3_TITLE_H

#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/LocalizationMapper/LocalizationMapper.h"
#include <memory>
#include <string>


extern commonItems::Color::Factory laFabricaDeColor;


namespace mappers
{
	class TagTitleMapper;
	class CoaMapper;
	class ProvinceMapper;
} // namespace mappers

namespace CK3
{
class Province;
class LandedTitles;
class TitlesHistory;
class Title: commonItems::parser, public std::enable_shared_from_this<Title>
{
  public:
	Title() = default;
	explicit Title(const std::string& name) { titleName = name; }
	void initializeFromTag(
		std::shared_ptr<Imperator::Country> theCountry, 
		mappers::LocalizationMapper& localizationMapper, 
		LandedTitles& landedTitles, 
		mappers::ProvinceMapper& provinceMapper,
		mappers::CoaMapper& coaMapper,
		mappers::TagTitleMapper& tagTitleMapper);
	void loadTitles(std::istream& theStream);

	void registerProvince(std::pair<unsigned long long, std::shared_ptr<Province>> theProvince) { provinces.insert(std::move(theProvince)); }
	void setLocalizations(const mappers::LocBlock& newBlock) { localizations[titleName] = newBlock; } // Setting the name
	void addCountyProvince(const unsigned long long provinceId) { countyProvinces.insert(provinceId); }
	void addHistory(const LandedTitles& landedTitles, TitlesHistory& titlesHistory);
	
	void setDeJureLiege(const std::shared_ptr<Title>& liegeTitle);
	void setDeFactoLiege(const std::shared_ptr<Title>& liegeTitle);

	[[nodiscard]] const auto& getName() const { return titleName; }

	[[nodiscard]] const auto& getDeJureLiege() const { return deJureLiege; }
	[[nodiscard]] const auto& getDeFactoLiege() const { return deFactoLiege; }
	
	[[nodiscard]] const auto& getDeJureVassals() const { return deJureVassals; }
	[[nodiscard]] const auto& getDeFactoVassals() const { return deFactoVassals; }
	[[nodiscard]] std::map<std::string, std::shared_ptr<Title>> getDeJureVassalsAndBelow(const std::string& rankFilter = "bcdke") const;
	[[nodiscard]] std::map<std::string, std::shared_ptr<Title>> getDeFactoVassalsAndBelow(const std::string& rankFilter = "bcdke") const;
	
	[[nodiscard]] const auto& getProvince() const { return province; } // for barony titles
	[[nodiscard]] const auto& getCountyProvinces() const { return countyProvinces; } // county titles

	bool generated = false; // if title is not based on CK3 landed titles file
	bool definiteForm = false;
	bool landless = false;
	std::string holder = "0"; // ID of Character holding the Title 
	std::map<std::string, mappers::LocBlock> localizations;
	std::optional<std::string> coa;
	std::optional<std::string> capitalCounty;
	std::shared_ptr<Imperator::Country> imperatorCountry;
	std::optional<commonItems::Color> color;
	std::string historyString = "1.1.1 = { holder = 0 }"; // this string is used in title history when title's holder is "0"
	
	std::pair<std::string, std::shared_ptr<Title>> capital;	// Capital county

	friend std::ostream& operator<<(std::ostream& output, const Title& title);
	
	// used by county titles only
	std::string capitalBarony; // used when parsing inside county to save first barony
	unsigned long long capitalBaronyProvince = 0;	// county barony's province; 0 is not a valid barony ID

  private:
	friend class LandedTitles;
	static void addFoundTitle(const std::shared_ptr<Title>& newTitle, std::map<std::string, std::shared_ptr<Title>>& foundTitles);
	
	void registerKeys();
	void trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper);

	std::string titleName; // e.g. d_latium
	
	std::optional<commonItems::Color> color1;
	std::optional<commonItems::Color> color2;

	std::shared_ptr<Title> deJureLiege; // direct de jure liege title name, e.g. e_hispania
	std::shared_ptr<Title> deFactoLiege; // direct de facto liege title name, e.g. e_hispania

	std::map<std::string, std::shared_ptr<Title>> deJureVassals; // DIRECT de jure vassals
	std::map<std::string, std::shared_ptr<Title>> deFactoVassals; // DIRECT de facto vassals
	
	std::map<unsigned long long, std::shared_ptr<Province>> provinces;
	std::map<std::string, std::shared_ptr<Title>> foundTitles;			// title name, title. Titles are only held here during loading of landed_titles, then they are cleared

	// used by barony titles only
	std::optional<unsigned long long> province; // province is area on map. b_ barony is its corresponding title.

	// used by county titles only
	std::set<unsigned long long> countyProvinces;
};
} // namespace CK3

#endif // CK3_TITLE_H
