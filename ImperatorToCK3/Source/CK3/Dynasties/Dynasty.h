#ifndef CK3_DYNASTY_H
#define CK3_DYNASTY_H



#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include <memory>



namespace Imperator {
class Family;
}

namespace CK3 {

class Dynasty {
public:
	Dynasty(const std::shared_ptr<Imperator::Family>& impFamily, const mappers::LocalizationMapper& locMapper);

	[[nodiscard]] const auto& getID() const { return ID; }
	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] const auto& getCulture() const { return culture; }
	[[nodiscard]] const auto& getLocalization() const { return localization; }

	friend std::ostream& operator<<(std::ostream& output, const Dynasty& dynasty);

private:
	std::string ID;
	std::string name;
	std::string culture;

	std::pair<std::string, mappers::LocBlock> localization;
};

} // namespace CK3

#endif // CK3_DYNASTY_H