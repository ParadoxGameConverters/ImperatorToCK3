#ifndef IMPERATOR_PROVINCE_H
#define IMPERATOR_PROVINCE_H
#include "Parser.h"

namespace Imperator
{
	class Pop;
	class Country;
	class Province: commonItems::parser
	{
	  public:
		Province() = default;
		Province(std::istream& theStream, int provID);

		[[nodiscard]] auto getID() const { return provinceID; }
		[[nodiscard]] const auto& getName() const { return name; }
		[[nodiscard]] const auto& getCulture() const { return culture; }
		[[nodiscard]] const auto& getReligion() const { return religion; }
		[[nodiscard]] const auto& getOwner() const { return owner; }
		[[nodiscard]] const auto& getController() const { return controller; }
		[[nodiscard]] const auto& getPops() const { return pops; }
		[[nodiscard]] auto getBuildingsCount() const { return buildingsCount; }

		[[nodiscard]] auto getPopCount() const { return static_cast<int>(pops.size()); }

		void setPops(const std::map<int, std::shared_ptr<Pop>>& newPops) { pops = newPops; }

		std::shared_ptr<Country> country;

	  private:
		void registerKeys();

		unsigned int provinceID = 0;
		std::string name;
		std::string culture;
		std::string religion;
		unsigned int owner = 0;
		unsigned int controller = 0;
		unsigned int buildingsCount = 0;
		std::map<int, std::shared_ptr<Pop>> pops;
	};
} // namespace Imperator

#endif // IMPERATOR_PROVINCE_H
