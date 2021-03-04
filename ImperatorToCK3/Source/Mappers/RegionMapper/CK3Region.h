#ifndef CK3_REGION_H
#define CK3_REGION_H



#include "Parser.h"
#include <set>



namespace CK3 {
class Title;
}

namespace mappers {

class CK3Region: commonItems::parser {
  public:
	explicit CK3Region(std::istream& theStream);

	[[nodiscard]] const auto& getRegions() const { return regions; }
	[[nodiscard]] const auto& getDuchies() const { return duchies; }
	[[nodiscard]] const auto& getCounties() const { return counties; }
	[[nodiscard]] const auto& getProvinces() const { return provinces; }
	[[nodiscard]] bool regionContainsProvince(unsigned long long province) const;

	void linkRegion(const std::string& regionName, const std::shared_ptr<CK3Region>& region);
	void linkDuchy(const std::shared_ptr<CK3::Title>& theDuchy);
	void linkCounty(const std::shared_ptr<CK3::Title>& theCounty);

  private:
	void registerKeys();
	std::map<std::string, std::shared_ptr<CK3Region>> regions;
	std::map<std::string, std::shared_ptr<CK3::Title>> duchies;
	std::map<std::string, std::shared_ptr<CK3::Title>> counties;
	std::set<unsigned long long> provinces;
};

} // namespace mappers



#endif // CK3_REGION_H