#ifndef CK3_REGION_H
#define CK3_REGION_H

#include "Parser.h"
#include <set>
#include "../../CK3/Titles/Title.h"
#include "Log.h"

namespace mappers
{
class Region: commonItems::parser
{
  public:
	explicit Region(std::istream& theStream);

	[[nodiscard]] const auto& getRegions() const { return regions; }
	[[nodiscard]] const auto& getDuchies() const { return duchies; }
	[[nodiscard]] const auto& getProvinces() const { return provinces; }
	[[nodiscard]] bool regionContainsProvince(unsigned long long province) const;

	void linkRegion(const std::pair<std::string, std::shared_ptr<Region>>& theRegion) { regions[theRegion.first] = theRegion.second; }
	void linkDuchy(const std::shared_ptr<CK3::Title>& theDuchy) { duchies[theDuchy->getName()] = theDuchy; }
	void linkCounty(const std::shared_ptr<CK3::Title>& theCounty) { counties[theCounty->getName()] = theCounty; }

  private:
	void registerKeys();
	std::map<std::string, std::shared_ptr<Region>> regions;
	std::map<std::string, std::shared_ptr<CK3::Title>> duchies;
	std::map<std::string, std::shared_ptr<CK3::Title>> counties;
	std::set<unsigned long long> provinces;
};
} // namespace mappers

#endif // CK3_REGION_H