#include "CountryName.h"



Imperator::CountryName::CountryName(const CountryName& other): name(other.name), adjective(other.adjective) {
	memcpy(&base, &other.base, sizeof std::unique_ptr<CountryName>);
}


Imperator::CountryName& Imperator::CountryName::operator=(const CountryName& other) noexcept {
	CountryName local(other);
	swap(*this, local);
	return *this;
}


[[nodiscard]] std::string Imperator::CountryName::getAdjective() const {
	if (adjective) {
		return *adjective;
	}
	return name + "_ADJ";
}
