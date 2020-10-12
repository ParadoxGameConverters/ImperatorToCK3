#ifndef IMPERATOR_CHARACTER_ATTRIBUTES_H
#define IMPERATOR_CHARACTER_ATTRIBUTES_H
#include "Parser.h"

namespace Imperator
{
class CharacterAttributes : commonItems::parser
{
  public:
	CharacterAttributes() = default;
	explicit CharacterAttributes(std::istream& theStream);

	[[nodiscard]] const auto& getMartial() const { return martial; }
	[[nodiscard]] const auto& getFinesse() const { return finesse; }
	[[nodiscard]] const auto& getCharisma() const { return charisma; }
	[[nodiscard]] const auto& getZeal() const { return zeal; }

  private:
	void registerKeys();

	int martial = 0;
	int finesse = 0;
	int charisma = 0;
	int zeal = 0;
};
} // namespace Imperator

#endif // IMPERATOR_CHARACTER_ATTRIBUTES_H