#include "CountryName.h"



[[nodiscard]] std::string Imperator::CountryName::getAdjective() const {
	if (adjective) {
		return *adjective;
	}
	return name + "_ADJ";
}
