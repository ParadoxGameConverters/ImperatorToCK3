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
class Title
{
  public:
	Title() = default;
	void initializeFromTag(
		std::shared_ptr<ImperatorWorld::Country> theCountry, 
		mappers::LocalizationMapper& localizationMapper, 
		LandedTitles& landedTitles, 
		mappers::ProvinceMapper& provinceMapper,
		mappers::CoaMapper& coaMapper,
		mappers::TagTitleMapper& tagTitleMapper);

	bool generated = false;
	std::string holder = "0";
	std::string titleName; // e.g. e_hispania
	std::string historyCountryFile;
	std::map<std::string, mappers::LocBlock> localizations;
	std::optional<std::string> coa;
	std::optional<std::string> capitalCounty;
	std::shared_ptr<ImperatorWorld::Country> imperatorCountry;
	std::string historyString = "1.1.1 = { holder = 0 }"; // this string is used in title history when title's holder is "0"

	void registerProvince(std::pair<int, std::shared_ptr<Province>> theProvince) { provinces.insert(std::move(theProvince)); }
	void setLocalizations(const mappers::LocBlock& newBlock) { localizations[titleName] = newBlock; } // Setting the name

	friend std::ostream& operator<<(std::ostream& output, const Title& title);

  private:
	void trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper);
	
	std::optional<commonItems::Color> color1;
	std::optional<commonItems::Color> color2;

	std::map<int, std::shared_ptr<Province>> provinces;
};
} // namespace CK3

#endif // CK3_TITLE_H
