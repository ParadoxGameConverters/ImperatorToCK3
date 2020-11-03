#ifndef CK3_TITLE_H
#define CK3_TITLE_H

#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../Titles/LandedTitles.h"
#include <memory>
#include <string>

namespace mappers
{
	class TagTitleMapper;
	class CoaMapper;
	class ProvinceMapper;
} // namespace mappers

namespace CK3
{
class Province;
class Title: commonItems::parser
{
  public:
	Title() = default;
	void initializeFromTag(
		std::shared_ptr<Imperator::Country> theCountry, 
		mappers::LocalizationMapper& localizationMapper, 
		LandedTitles& landedTitles, 
		mappers::ProvinceMapper& provinceMapper,
		mappers::CoaMapper& coaMapper,
		mappers::TagTitleMapper& tagTitleMapper);

	bool generated = false;
	std::string holder = "0";
	std::string titleName; // e.g. d_latium
	std::map<std::string, mappers::LocBlock> localizations;
	std::optional<std::string> coa;
	std::optional<std::string> capitalCounty;
	std::shared_ptr<Imperator::Country> imperatorCountry;
	std::string historyString = "1.1.1 = { holder = 0 }"; // this string is used in title history when title's holder is "0"

	void registerProvince(std::pair<unsigned long long, std::shared_ptr<Province>> theProvince) { provinces.insert(std::move(theProvince)); }
	void setLocalizations(const mappers::LocBlock& newBlock) { localizations[titleName] = newBlock; } // Setting the name

	friend std::ostream& operator<<(std::ostream& output, const Title& title);

	// =========================================================================================================
	// taken from LandedTitles

	void loadTitles(std::istream& theStream);

	void addCountyProvince(const unsigned long long provinceId) { countyProvinces.insert(provinceId); }
	
	[[nodiscard]] const auto& getProvince() const { return province; } // for barony titles
	[[nodiscard]] const auto& getCountyProvinces() const { return countyProvinces; } // county titles
	
	bool definiteForm = false;
	bool landless = false;
	std::optional<commonItems::Color> color;
	std::string capitalBarony; // used for county titles only; used when parsing inside county to save first barony
	std::pair<std::string, std::shared_ptr<Title>> capital;	// Capital county

	std::map<std::string, Title> foundTitles;			// title name, title

	unsigned long long capitalBaronyProvince = 0;	// Capital barony (for counties), 0 is not a valid barony ID

	std::shared_ptr<Title> deFactoLiege; // direct de facto liege title name, e.g. e_hispania
	std::shared_ptr<Title> deJureLiege; // direct de jure liege title name, e.g. e_hispania
	std::set<std::shared_ptr<Title>> deJureVassals; // DIRECT de jure vassals (NOT the same as foundTitles, which stores direct and INDIRECT de jure vassals)
	

  private:
	void trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper);
	
	std::optional<commonItems::Color> color1;
	std::optional<commonItems::Color> color2;

	std::map<unsigned long long, std::shared_ptr<Province>> provinces;

	// =========================================================================================================
	// taken from LandedTitles
	void registerKeys();
	std::optional<unsigned long long> province; // used for barony titles only; province is area on map. b_ barony is its corresponding title.
	std::set<unsigned long long> countyProvinces;

};
} // namespace CK3

#endif // CK3_TITLE_H
