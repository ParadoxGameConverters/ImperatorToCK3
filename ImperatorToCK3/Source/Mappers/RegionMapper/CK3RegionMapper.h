#ifndef CK3_REGIONS_H
#define CK3_REGIONS_H

#include "CK3/Titles/Title.h"
#include "CK3/Titles/LandedTitles.h"
#include "CK3Region.h"
#include "Parser.h"
#include <map>

namespace mappers
{
class CK3RegionMapper: commonItems::parser
{
  public:
	CK3RegionMapper() = default;

	void loadRegions(const std::string& ck3Path, CK3::LandedTitles& landedTitles);
	void loadRegions(CK3::LandedTitles& landedTitles, std::istream& regionStream, std::istream& islandRegionStream);

	[[nodiscard]] bool provinceIsInRegion(unsigned long long province, const std::string& regionName) const;
	[[nodiscard]] bool regionNameIsValid(const std::string& regionName) const;

	[[nodiscard]] std::optional<std::string> getParentCountyName(unsigned long long provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentDuchyName(unsigned long long provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentRegionName(unsigned long long provinceID) const;

  private:
	void registerRegionKeys();
	void linkRegions();

	std::map<std::string, std::shared_ptr<CK3Region>> regions;
	std::map<std::string, std::shared_ptr<CK3::Title>> duchies;
	std::map<std::string, std::shared_ptr<CK3::Title>> counties;
};
} // namespace mappers

#endif // CK3_REGIONS_H
