#ifndef CK3_COUNTY_H
#define CK3_COUNTY_H

#include "Parser.h"
#include "Barony.h"
#include <map>
#include <memory>
#include <set>

namespace CK3
{
class Province;
}
namespace mappers
{
class County: commonItems::parser
{
  public:
	explicit County(std::istream& theStream);

	[[nodiscard]] const auto& getProvinces() const { return provinces; }
	[[nodiscard]] bool countyContainsProvince(int province) const;

	void linkProvince(const std::pair<int, std::shared_ptr<CK3::Province>>& theProvince) { provinces[theProvince.first] = theProvince.second; }

  private:
	void registerKeys();
	std::map<int, std::shared_ptr<CK3::Province>> provinces;
	//std::set<unsigned int> provinces;
};
} // namespace mappers

#endif // CK3_COUNTY_H