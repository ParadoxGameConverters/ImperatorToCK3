#ifndef EU4_REGIONS_H
#define EU4_REGIONS_H

#include "Region.h"
#include "Duchy.h"
#include "County.h"
#include "Parser.h"
#include <map>

class Configuration;
namespace mappers
{
class RegionMapper: commonItems::parser
{
  public:
	RegionMapper() = default;
	virtual ~RegionMapper() = default;
	RegionMapper(const RegionMapper&) = default;
	RegionMapper& operator=(const RegionMapper&) = default;
	RegionMapper(RegionMapper&&) = default;
	RegionMapper& operator=(RegionMapper&&) = default;

	void loadRegions(const Configuration& theConfiguration);
	void loadRegions(std::istream& landedTitlesStream, std::istream& regionStream, std::istream& islandRegionStream); // for testing

	[[nodiscard]] bool provinceIsInRegion(int province, const std::string& regionName) const;
	[[nodiscard]] bool regionNameIsValid(const std::string& regionName) const;

	[[nodiscard]] std::optional<std::string> getParentCountyName(int provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentDuchyName(int provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentRegionName(int provinceID) const;

	//void linkRegionsToRegions(const std::map<int, std::shared_ptr<Region>>& theRegions);
	//void linkProvinces(const std::map<int, std::shared_ptr<CK3::Province>>& theProvinces);

  private:
	void loadLandedTitles(std::istream& theStream);
	void registerLandedTitlesKeys();
	void registerRegionKeys();
	void linkRegions();

	std::map<std::string, std::shared_ptr<Region>> regions;
	std::map<std::string, std::shared_ptr<Duchy>> duchies;
	std::map<std::string, std::shared_ptr<County>> counties;
};
} // namespace mappers

#endif // EU4_REGIONS_H
