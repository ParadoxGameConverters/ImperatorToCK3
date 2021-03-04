#ifndef PROVINCE_MAPPER_H
#define PROVINCE_MAPPER_H



#include "ProvinceMappingsVersion.h"
#include "Parser.h"
#include <map>
#include <set>



class Configuration;

namespace mappers {

class ProvinceMapper: commonItems::parser {
  public:
	ProvinceMapper();
	explicit ProvinceMapper(std::istream& theStream);

	[[nodiscard]] std::vector<unsigned long long> getImperatorProvinceNumbers(unsigned long long ck3ProvinceNumber) const;
	[[nodiscard]] std::vector<unsigned long long> getCK3ProvinceNumbers(unsigned long long impProvinceNumber) const;
	[[nodiscard]] auto isValidCK3Province(const unsigned long long ck3Province) const { return validCK3Provinces.count(ck3Province) > 0; }

	void determineValidProvinces(const Configuration& theConfiguration);

  private:
	void registerKeys();
	void createMappings();

	std::map<unsigned long long, std::vector<unsigned long long>> ImpToCK3ProvinceMap;
	std::map<unsigned long long, std::vector<unsigned long long>> CK3ToImpProvinceMap;
	std::set<unsigned long long> validCK3Provinces;
	ProvinceMappingsVersion theMappings;
};

} // namespace mappers



#endif // PROVINCE_MAPPER_H