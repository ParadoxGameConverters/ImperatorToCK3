#ifndef RELIGION_MAPPING_H
#define RELIGION_MAPPING_H



#include "Parser.h"
#include <set>



namespace mappers {

class ImperatorRegionMapper;
class CK3RegionMapper;
class ReligionMapping: commonItems::parser {
  public:
	explicit ReligionMapping(std::istream& theStream);

	void insertImperatorRegionMapper(const std::shared_ptr<ImperatorRegionMapper>& impRegionMapper) { imperatorRegionMapper = impRegionMapper; }
	void insertCK3RegionMapper(const std::shared_ptr<CK3RegionMapper>& CK3RegionMapper) { ck3RegionMapper = CK3RegionMapper; }

	[[nodiscard]] std::optional<std::string> match(const std::string& impReligion, unsigned long long ck3ProvinceID, unsigned long long impProvinceID) const; // ID 0 means no province

  private:
	std::set<std::string> impReligions;
	std::string ck3Religion;
	
	std::set<unsigned long long> imperatorProvinces;
	std::set<unsigned long long> ck3Provinces;
	
	std::set<std::string> imperatorRegions;
	std::set<std::string> ck3Regions;

	std::shared_ptr<ImperatorRegionMapper> imperatorRegionMapper;
	std::shared_ptr<CK3RegionMapper> ck3RegionMapper;
};

} // namespace mappers



#endif // RELIGION_MAPPING_H