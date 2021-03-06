#ifndef IMPERATOR_CHARACTER_NAME_H
#define IMPERATOR_CHARACTER_NAME_H
#include "Parser.h"

namespace Imperator
{
class CharacterName: commonItems::parser
{
  public:
	CharacterName() = default;
	explicit CharacterName(std::istream& theStream);

	[[nodiscard]] const auto& getName() const { return name; }

  private:
	void registerKeys();

	std::string name;
};
} // namespace Imperator

#endif // IMPERATOR_CHARACTER_NAME_H