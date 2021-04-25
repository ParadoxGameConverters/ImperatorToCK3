#include "CountryName.h"
#include "Country.h"
#include <ranges>



template<class T>
std::unique_ptr<T> copy_unique(const std::unique_ptr<T>& source) {
	return source ? std::make_unique<T>(*source) : nullptr;
}


void replaceAllOccurencesInString(std::string& modifiedString, const std::string& substring, const std::string& replacement) {
	size_t index = 0;
	const auto size = substring.size();
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


std::optional<mappers::LocBlock> Imperator::CountryName::getNameLocBlock(mappers::LocalizationMapper& localizationMapper,
																		 const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries) const {
	auto directNameLocMatch = localizationMapper.getLocBlockForKey(name);
	if (directNameLocMatch && name == "CIVILWAR_FACTION_NAME") {
		// special case for revolts
		if (base) {
			std::optional<mappers::LocBlock> baseAdjLoc = base->getAdjectiveLocBlock(localizationMapper, imperatorCountries);
			if (baseAdjLoc) {
				directNameLocMatch->modifyForEveryLanguage(*baseAdjLoc,
														   [](std::string& orig, const std::string& modifying) { replaceAllOccurencesInString(orig, "$ADJ$", modifying); });
				return directNameLocMatch;
			}
		}
	}
	return directNameLocMatch;
}

std::optional<mappers::LocBlock> Imperator::CountryName::getAdjectiveLocBlock(mappers::LocalizationMapper& localizationMapper,
																			  const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries) const {
	const auto adj = getAdjective();
	auto directAdjLocMatch = localizationMapper.getLocBlockForKey(adj);

	if (directAdjLocMatch && adj == "CIVILWAR_FACTION_ADJECTIVE") {
		// special case for revolts
		if (base) {
			auto baseAdjLoc = base->getAdjectiveLocBlock(localizationMapper, imperatorCountries);
			if (baseAdjLoc) {
				directAdjLocMatch->modifyForEveryLanguage(*baseAdjLoc,
														   [](std::string& orig, const std::string& modifying) { replaceAllOccurencesInString(orig, "$ADJ$", modifying); });
				return directAdjLocMatch;
			}
		}
	}
	else {
		for (const auto& country : imperatorCountries | std::ranges::views::values) {
			if (country->getName() == name) {
				const auto countryAdjective = country->getCountryName().getAdjective();
				auto adjLoc = localizationMapper.getLocBlockForKey(countryAdjective);
				if (adjLoc) {
					return adjLoc;
				}
			}
		}
	}

	if (!name.empty()) { // as fallback, use country name (which is apparently what Imperator does)
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(name);
		if (adjLocalizationMatch) {
			return *adjLocalizationMatch;
		}
	}
	return directAdjLocMatch;
}
