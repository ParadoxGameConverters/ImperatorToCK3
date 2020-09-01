#ifndef PROVINCE_MAPPER_H
#define PROVINCE_MAPPER_H

#include "ProvinceMappingsVersion.h"
#include "Parser.h"
#include <map>
#include <set>

class Configuration;

namespace mappers
{
class ProvinceMapper: commonItems::parser
{
  public:
	ProvinceMapper();
	explicit ProvinceMapper(std::istream& theStream);

	[[nodiscard]] std::vector<int> getImpProvinceNumbers(int ck3ProvinceNumber) const;
	[[nodiscard]] std::vector<int> getCK3ProvinceNumbers(int impProvinceNumber) const;
	[[nodiscard]] auto isValidEU4Province(const int eu4Province) const { return validCK3Provinces.count(eu4Province) > 0; }

	void determineValidProvinces(const Configuration& theConfiguration);

  private:
	void registerKeys();
	void createMappings();

	std::map<int, std::vector<int>> ImpToCK3ProvinceMap;
	std::map<int, std::vector<int>> CK3ToImpProvinceMap;
	std::set<int> validCK3Provinces;
	ProvinceMappingsVersion theMappings;
};
} // namespace mappers

#endif // PROVINCE_MAPPER_H