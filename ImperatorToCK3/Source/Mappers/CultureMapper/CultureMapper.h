#ifndef CULTURE_MAPPER_H
#define CULTURE_MAPPER_H



#include "CultureMappingRule.h"
#include "Parser.h"
#include <optional>
#include <string>



namespace mappers {

class CultureMapper: commonItems::parser {
  public:
	CultureMapper();
	explicit CultureMapper(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> match(const std::string& impCulture,
		const std::string& ck3religion,
		unsigned long long ck3ProvinceID,
		unsigned long long impProvinceID,
		const std::string& ck3ownerTitle) const;

	[[nodiscard]] std::optional<std::string> nonReligiousMatch(const std::string& impCulture,
		const std::string& ck3religion,
		unsigned long long ck3ProvinceID,
		unsigned long long impProvinceID,
		const std::string& ck3ownerTitle) const;

	void loadRegionMappers(std::shared_ptr<ImperatorRegionMapper> impRegionMapper, std::shared_ptr<CK3RegionMapper> _ck3RegionMapper);

  private:
	void registerKeys();

	std::vector<CultureMappingRule> cultureMapRules;
};

} // namespace mappers



#endif // CULTURE_MAPPER_H