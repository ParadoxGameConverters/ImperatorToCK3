#include "CountryName.h"
#include "Country.h"
#include <ranges>
#include "Log.h"



template<class T>
std::unique_ptr<T> copy_unique(const std::unique_ptr<T>& source) {
	return source ? std::make_unique<T>(*source) : nullptr;
}


void replaceAllOccurencesInString(std::string& modifiedString, const std::string& substring, const std::string& replacement) {
	size_t index = 0;
	size_t size = substring.size();
	while (true) {
		// Locate the substring to replace.
		index = modifiedString.find(substring, index);
		if (index == std::string::npos)
			return;

		// Make the replacement.
		modifiedString.replace(index, size, replacement);

		// Advance index forward so the next iteration doesn't pick it up as well.
		index += size;
	}
}


Imperator::CountryName::CountryName(const CountryName& other): name(other.name), adjective(other.adjective), base(copy_unique(other.base)) {}


Imperator::CountryName::CountryName(CountryName&& other) noexcept: name(std::move(other.name)), adjective(std::move(other.adjective)), base(std::move(other.base))  {}


Imperator::CountryName& Imperator::CountryName::operator=(const CountryName& other) {
	name = other.name;
	adjective = other.adjective;
	base = copy_unique(other.base);
	return *this;
}


Imperator::CountryName& Imperator::CountryName::operator=(CountryName&& other) noexcept {
	name = std::move(other.name);
	adjective = std::move(other.adjective);
	base = std::move(other.base);
	return *this;
}


[[nodiscard]] std::string Imperator::CountryName::getAdjective() const {
	if (adjective) {
		return *adjective;
	}
	return name + "_ADJ";
}


std::optional<mappers::LocBlock> Imperator::CountryName::getAdjectiveLocBlock(mappers::LocalizationMapper& localizationMapper, const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries, const std::shared_ptr<Imperator::Country> imperatorCountry) const {
	auto adj = getAdjective();
	auto directAdjLocMatch = localizationMapper.getLocBlockForKey(adj);
	if (adj == "CIVILWAR_FACTION_ADJECTIVE") {
		// special case for revolts
		if (base) {
			std::optional<mappers::LocBlock> baseAdjLoc;
			if (base->getAdjective() == "CIVILWAR_FACTION_ADJECTIVE") { // revolt of revolt
				baseAdjLoc = base->getAdjectiveLocBlock(localizationMapper, imperatorCountries, imperatorCountry);
			}
			else {
				for (const auto& country : imperatorCountries | std::ranges::views::values) {
					if (country->getName() == base->getName()) {
						const auto baseCountryAdjective = country->getCountryName().getAdjective();
						baseAdjLoc = localizationMapper.getLocBlockForKey(baseCountryAdjective);
						break;
					}
				}
			}
			if (baseAdjLoc) {
				replaceAllOccurencesInString(directAdjLocMatch->english, "$ADJ$", baseAdjLoc->english);
				replaceAllOccurencesInString(directAdjLocMatch->french, "$ADJ$", baseAdjLoc->french);
				replaceAllOccurencesInString(directAdjLocMatch->german, "$ADJ$", baseAdjLoc->german);
				replaceAllOccurencesInString(directAdjLocMatch->russian, "$ADJ$", baseAdjLoc->russian);
				replaceAllOccurencesInString(directAdjLocMatch->spanish, "$ADJ$", baseAdjLoc->spanish);
				return directAdjLocMatch;
			}
		}
	}
	return directAdjLocMatch;
}
