#ifndef PROVINCE_MAPPING_H
#define PROVINCE_MAPPING_H



#include "Parser.h"



namespace mappers {

class ProvinceMapping: commonItems::parser {
  public:
	ProvinceMapping() = default;
	explicit ProvinceMapping(std::istream& theStream);

	[[nodiscard]] const auto& getCK3Provinces() const { return ck3Provinces; }
	[[nodiscard]] const auto& getImpProvinces() const { return impProvinces; }

  private:
	void registerKeys();

	std::vector<unsigned long long> ck3Provinces;
	std::vector<unsigned long long> impProvinces;
};

} // namespace mappers



#endif // PROVINCE_MAPPING_H