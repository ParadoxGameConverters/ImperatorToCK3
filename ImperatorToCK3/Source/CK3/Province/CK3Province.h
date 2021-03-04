#ifndef CK3_PROVINCE_H
#define CK3_PROVINCE_H



#include "ProvinceDetails.h"
#include <memory>
#include <string>



namespace Imperator {
class Province;
} // namespace Imperator

namespace mappers {
class CultureMapper;
class ReligionMapper;
} // namespace mappers


namespace CK3 {

class Title;
class Province {
  public:
	Province() = default;
	Province(unsigned long long id, std::istream& theStream);
	Province(unsigned long long id, const Province& otherProv);

	void initializeFromImperator(const std::shared_ptr<Imperator::Province>& origProvince,
	                             const mappers::CultureMapper& cultureMapper,
	                             const mappers::ReligionMapper& religionMapper);

	[[nodiscard]] auto getID() const { return ID; }
	[[nodiscard]] const auto& getReligion() const { return details.religion; }
	[[nodiscard]] const auto& getCulture() const { return details.culture; }
	[[nodiscard]] const auto& getHolding() const { return details.holding; }
	[[nodiscard]] const auto& getImperatorProvince() const { return imperatorProvince; }


	void setReligion(const std::string& religion) { details.religion = religion; }
	void setImperatorProvince(const std::shared_ptr<Imperator::Province>& impProvPtr) { imperatorProvince = impProvPtr; }

	friend std::ostream& operator<<(std::ostream& output, const Province& province);

  private:
	  void setReligion(const mappers::ReligionMapper& religionMapper);
	  void setCulture(const mappers::CultureMapper& cultureMapper);
	  void setHolding();
	
	  unsigned long long ID = 0;
	  ProvinceDetails details;
	  std::pair<std::string, std::shared_ptr<Title>> titleCountry;

	  std::shared_ptr<Imperator::Province> imperatorProvince;
};

} // namespace CK3



#endif // CK3_PROVINCE_H