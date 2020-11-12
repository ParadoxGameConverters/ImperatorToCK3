#ifndef CK3_REGION_H
#define CK3_REGION_H

#include "Parser.h"
#include <set>

namespace mappers
{
class Duchy;
class County;
class Region: commonItems::parser
{
  public:
	explicit Region(std::istream& theStream);

	[[nodiscard]] const auto& getRegions() const { return regions; }
	[[nodiscard]] const auto& getDuchies() const { return duchies; }
	[[nodiscard]] const auto& getProvinces() const { return provinces; }
	[[nodiscard]] bool regionContainsProvince(int province) const;

	void linkRegion(const std::pair<std::string, std::shared_ptr<Region>>& theRegion) { regions[theRegion.first] = theRegion.second; }
	void linkDuchy(const std::pair<std::string, std::shared_ptr<Duchy>>& theDuchy) { duchies[theDuchy.first] = theDuchy.second; }
	//void linkCounty(const std::pair<std::string, std::shared_ptr<County>>& theCounty) { counties[theCounty.first] = theCounty.second; }

  private:
	void registerKeys();
	std::map<std::string, std::shared_ptr<Region>> regions;
	std::map<std::string, std::shared_ptr<Duchy>> duchies;
	//std::map<std::string, std::shared_ptr<County>> counties;
	std::set<unsigned int> provinces;
};
} // namespace mappers

#endif // CK3_REGION_H