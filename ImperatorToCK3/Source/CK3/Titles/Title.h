#ifndef CK3_TITLE_H
#define CK3_TITLE_H

#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../Titles/LandedTitles.h"
#include <memory>
#include <string>

namespace mappers
{
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
	void initializeFromTag(std::string theTitle,
		std::shared_ptr<ImperatorWorld::Country> theCountry, 
		mappers::LocalizationMapper& localizationMapper, 
		LandedTitles& landedTitles, 
		mappers::ProvinceMapper& provinceMapper,
		mappers::CoaMapper& coaMapper);

	[[nodiscard]] const auto& getTitleName() const { return titleName; }
	[[nodiscard]] const auto& getHistoryCountryFile() const { return historyCountryFile; }
	[[nodiscard]] const auto& getLocalizations() const { return localizations; }
	[[nodiscard]] const auto& getCoa() const { return coa; }
	[[nodiscard]] auto getCapitalCounty() const { return capitalCounty; }
	[[nodiscard]] const auto& getImperatorCountry() const { return imperatorCountry; }

	void registerProvince(std::pair<int, std::shared_ptr<Province>> theProvince) { provinces.insert(std::move(theProvince)); }
	void setLocalizations(const mappers::LocBlock& newBlock) { localizations[titleName] = newBlock; } // Setting the name

	friend std::ostream& operator<<(std::ostream& output, const Title& versionParser);

  private:
	std::string titleName; // e.g. e_hispania
	std::string historyCountryFile;

	int holder = -1;
	commonItems::Color color1;
	commonItems::Color color2;
	std::optional<std::string> coa;
	std::optional<std::string> capitalCounty;

	std::map<std::string, mappers::LocBlock> localizations;

	std::pair<std::string, std::shared_ptr<ImperatorWorld::Country>> imperatorCountry;
	std::map<int, std::shared_ptr<Province>> provinces;
};
} // namespace CK3

#endif // CK3_TITLE_H
