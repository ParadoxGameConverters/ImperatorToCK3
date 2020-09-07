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
class Country;
class Province
{
  public:
	Province() = default;

	Province(int id, std::istream& theStream);

	void updateWith(const std::string& filePath);
	void initializeFromImperator(std::shared_ptr<ImperatorWorld::Province> origProvince,
		 const mappers::CultureMapper& cultureMapper,
		 const mappers::ReligionMapper& religionMapper);

	[[nodiscard]] const auto& getTagCountry() const { return tagCountry; }
	//[[nodiscard]] const auto& getOwner() const { return details.owner; }
	[[nodiscard]] const auto& getReligion() const { return details.religion; }
	[[nodiscard]] const auto& getCulture() const { return details.culture; }
	[[nodiscard]] const auto& getSourceProvince() const { return srcProvince; }
	[[nodiscard]] auto getProvinceID() const { return provID; }
	

	void registerTagCountry(const std::pair<std::string, std::shared_ptr<Country>>& theCountry) { tagCountry = theCountry; }
	//void setOwner(const std::string& tag) { details.owner = tag; }
	//void setController(const std::string& tag) { details.controller = tag; }
	void setReligion(const std::string& religion) { details.religion = religion; }
	void sterilize();

	friend std::ostream& operator<<(std::ostream& output, const Province& versionParser);

  private:
	int provID = 0;
	std::shared_ptr<ImperatorWorld::Province> srcProvince;
	ProvinceDetails details;
	std::pair<std::string, std::shared_ptr<Country>> tagCountry;
};
} // namespace CK3

#endif // CK3_PROVINCE_H