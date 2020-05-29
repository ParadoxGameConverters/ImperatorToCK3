#ifndef IMPERATOR_PROVINCE_NAME_H
#define IMPERATOR_PROVINCE_NAME_H
#include "Parser.h"

namespace ImperatorWorld
{
class ProvinceName: commonItems::parser
{
  public:
	ProvinceName() = default;
	explicit ProvinceName(std::istream& theStream);

	[[nodiscard]] const auto& getName() const { return name; }

  private:
	void registerKeys();

	std::string name;
};
} // namespace ImperatorWorld

#endif // IMPERATOR_PROVINCE_NAME_H