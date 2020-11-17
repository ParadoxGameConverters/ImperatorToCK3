#ifndef IMPERATOR_REGION_MAPPER_H
#define IMPERATOR_REGION_MAPPER_H

#include "Parser.h"
#include <map>
#include "ImperatorRegion.h"

namespace mappers
{
class ImperatorRegionMapper: commonItems::parser
{
  public:
	ImperatorRegionMapper() = default;
	explicit ImperatorRegionMapper(const std::string& imperatorPath);
	void loadRegions(std::istream& areaStream, std::istream& regionStream);

	[[nodiscard]] bool provinceIsInRegion(unsigned long long province, const std::string& regionName) const;
	[[nodiscard]] bool regionNameIsValid(const std::string& regionName) const;

	[[nodiscard]] std::optional<std::string> getParentRegionName(unsigned long long provinceID) const;
	[[nodiscard]] std::optional<std::string> getParentAreaName(unsigned long long provinceID) const;

  private:
	void registerRegionKeys();
	void registerAreaKeys();
	void linkRegions();

	std::map<std::string, std::shared_ptr<ImperatorRegion>> regions;
	std::map<std::string, std::shared_ptr<ImperatorArea>> areas;
};
} // namespace mappers

#endif // IMPERATOR_REGION_MAPPER_H
