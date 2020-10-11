#ifndef IMPERATOR_PROVINCES_H
#define IMPERATOR_PROVINCES_H
#include "Parser.h"

namespace ImperatorWorld
{
	class Province;
	class Pops;
	class Countries;
	class Provinces: commonItems::parser
	{
	  public:
		Provinces() = default;
		explicit Provinces(std::istream& theStream);
		[[nodiscard]] const auto& getProvinces() const { return provinces; }

		void linkPops(const Pops& thePops);
		void linkCountries(const Countries& theCountries);

	  private:
		void registerKeys();

		std::map<int, std::shared_ptr<Province>> provinces;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_PROVINCES_H
