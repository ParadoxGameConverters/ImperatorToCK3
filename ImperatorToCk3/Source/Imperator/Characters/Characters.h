#ifndef IMPERATOR_CHARACTERS_H
#define IMPERATOR_CHARACTERS_H
#include "Parser.h"

namespace ImperatorWorld
{
class Families;
class Character;
class Characters: commonItems::parser
{
  public:
	Characters() = default;
	Characters(std::istream& theStream);

	[[nodiscard]] const auto& getCharacters() const { return characters; }

	void linkFamilies(const Families& theFamilies);
	void linkSpouses();
	void linkMothersAndFathers();

  private:
	void registerKeys();

	std::map<int, std::shared_ptr<Character>> characters;
};
} // namespace ImperatorWorld

#endif // IMPERATOR_CHARACTERS_H
