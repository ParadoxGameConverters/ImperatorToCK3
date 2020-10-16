#ifndef CK3_PROVINCE_H
#define CK3_PROVINCE_H

#include "ProvinceDetails.h"
#include <memory>
#include <string>

namespace Imperator
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

	Province(unsigned long long id, std::istream& theStream);

	void updateWith(const std::string& filePath);
	void initializeFromImperator(const std::shared_ptr<Imperator::Province>& origProvince,
	                             const mappers::CultureMapper& cultureMapper,
	                             const mappers::ReligionMapper& religionMapper);

	[[nodiscard]] const auto& getTitleCountry() const { return titleCountry; }
	[[nodiscard]] const auto& getReligion() const { return details.religion; }
	[[nodiscard]] const auto& getCulture() const { return details.culture; }
	[[nodiscard]] auto getProvinceID() const { return provID; }
	

	void registerTitleCountry(const std::pair<std::string, std::shared_ptr<Title>>& theTitle) { titleCountry = theTitle; }
	void setReligion(const std::string& religion) { details.religion = religion; }

	std::shared_ptr<Imperator::Province> srcProvince;

	friend std::ostream& operator<<(std::ostream& output, const Province& province);

  private:
	  void setReligion(const mappers::ReligionMapper& religionMapper);
	  void setCulture(const mappers::CultureMapper& cultureMapper);
	
	  unsigned long long provID = 0;
	  ProvinceDetails details;
	  std::pair<std::string, std::shared_ptr<Title>> titleCountry;
};
} // namespace CK3

#endif // CK3_PROVINCE_H