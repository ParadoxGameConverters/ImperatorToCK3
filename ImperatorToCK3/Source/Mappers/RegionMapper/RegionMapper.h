#ifndef CK3_REGIONS_H
#define CK3_REGIONS_H

#include "../../CK3/Titles/Title.h"
#include "../../CK3/Titles/LandedTitles.h"
#include "Region.h"
#include "Parser.h"
#include <map>

class Configuration;
namespace mappers
{
class RegionMapper: commonItems::parser
{
  public:
	RegionMapper() = default;

	void loadRegions(const Configuration& theConfiguration, CK3::LandedTitles& landedTitles);
	void loadRegions(CK3::LandedTitles& landedTitles, std::istream& regionStream, std::istream& islandRegionStream); // for testing

	[[nodiscard]] bool provinceIsInRegion(unsigned long long province, const std::string& regionName) const;
	[[nodiscard]] bool regionNameIsValid(const std::string& regionName) const;

	[[nodiscard]] std::optional<std::string> getParentCountyName(unsigned long long provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentDuchyName(unsigned long long provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentRegionName(unsigned long long provinceID) const;

	//void linkRegionsToRegions(const std::map<int, std::shared_ptr<Region>>& theRegions);
	//void linkProvinces(const std::map<int, std::shared_ptr<CK3::Province>>& theProvinces);

  private:
	void registerRegionKeys();
	void linkRegions();

	std::map<std::string, std::shared_ptr<Region>> regions;
	std::map<std::string, std::shared_ptr<CK3::Title>> duchies;
	std::map<std::string, std::shared_ptr<CK3::Title>> counties;
};
} // namespace mappers

#endif // CK3_REGIONS_H
