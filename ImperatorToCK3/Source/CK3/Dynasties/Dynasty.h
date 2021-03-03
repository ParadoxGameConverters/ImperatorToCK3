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
	[[nodiscard]] const auto& getLocalization() const { return localization; }

private:
	std::string ID;
	std::string name;
	std::optional<std::string> culture;

	std::pair<std::string, mappers::LocBlock> localization;
};

} // namespace CK3

#endif // CK3_DYNASTY_H