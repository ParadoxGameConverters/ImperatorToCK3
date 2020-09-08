#ifndef CK3_PROVINCE_H
#define CK3_PROVINCE_H

#include "ProvinceDetails.h"
#include <memory>
#include <string>

namespace ImperatorWorld
{
class Province;
}
namespace mappers
{
class CultureMapper;
class ReligionMapper;
} // namespace mappers

namespace CK3
{
class Title;
class Province
{
  public:
	Province() = default;

	Province(int id, std::istream& theStream);

	void updateWith(const std::string& filePath);
	void initializeFromImperator(std::shared_ptr<ImperatorWorld::Province> origProvince,
		 const mappers::CultureMapper& cultureMapper,
		 const mappers::ReligionMapper& religionMapper);

	[[nodiscard]] const auto& getTitleCountry() const { return titleCountry; }
	//[[nodiscard]] const auto& getOwner() const { return details.owner; }
	[[nodiscard]] const auto& getReligion() const { return details.religion; }
	[[nodiscard]] const auto& getCulture() const { return details.culture; }
	[[nodiscard]] auto getProvinceID() const { return provID; }
	

	void registerTitleCountry(const std::pair<std::string, std::shared_ptr<Title>>& theTitle) { titleCountry = theTitle; }
	//void setOwner(const std::string& title) { details.owner = title; }
	//void setController(const std::string& title) { details.controller = title; }
	void setReligion(const std::string& religion) { details.religion = religion; }

	friend std::ostream& operator<<(std::ostream& output, const Province& versionParser);

  private:
	int provID = 0;
	std::shared_ptr<ImperatorWorld::Province> srcProvince;
	ProvinceDetails details;
	std::pair<std::string, std::shared_ptr<Title>> titleCountry;
};
} // namespace CK3

#endif // CK3_PROVINCE_H