#ifndef CULTURE_MAPPING_RULE_H
#define CULTURE_MAPPING_RULE_H

#include "Parser.h"
#include <set>

namespace mappers
{
class CultureMappingRule: commonItems::parser
{
  public:
	CultureMappingRule() = default;
	explicit CultureMappingRule(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> cultureMatch(const std::string& impCulture,
		 const std::string& CK3religion,
		unsigned long long CK3Province,
		 const std::string& CK3ownerTitle) const;

	[[nodiscard]] std::optional<std::string> cultureNonReligiousMatch(const std::string& impCulture,
		const std::string& CK3religion,
		unsigned long long CK3Province,
		const std::string& CK3ownerTitle) const;


	[[nodiscard]] const auto& getCK3Culture() const { return destinationCulture; } // for testing
	[[nodiscard]] const auto& getImperatorCultures() const { return cultures; }			 // for testing
	[[nodiscard]] const auto& getReligions() const { return religions; }				 // for testing
	[[nodiscard]] const auto& getOwners() const { return owners; }						 // for testing
	[[nodiscard]] const auto& getProvinces() const { return provinces; }				 // for testing

  private:
	std::string destinationCulture;
	std::set<std::string> cultures;
	std::set<std::string> religions;
	std::set<std::string> owners;
	std::set<unsigned long long> provinces;
};
} // namespace mappers

#endif // CULTURE_MAPPING_RULE_H
