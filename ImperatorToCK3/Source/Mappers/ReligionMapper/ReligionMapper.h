#ifndef RELIGION_MAPPER_H
#define RELIGION_MAPPER_H

#include "Parser.h"
#include "ReligionMapping.h"
#include <map>
#include <optional>
#include <string>

namespace mappers
{
class ReligionMapper: commonItems::parser
{
  public:
	ReligionMapper();
	explicit ReligionMapper(std::istream& theStream);

	void loadRegionMappers(std::shared_ptr<ImperatorRegionMapper> impRegionMapper, std::shared_ptr<CK3RegionMapper> _ck3RegionMapper);
	
	[[nodiscard]] std::optional<std::string> match(const std::string& impReligion, unsigned long long ck3ProvinceID, unsigned long long impProvinceID) const;

  private:
	void registerKeys();

	std::vector<ReligionMapping> religionMappings;
};
} // namespace mappers

#endif // RELIGION_MAPPER_H
