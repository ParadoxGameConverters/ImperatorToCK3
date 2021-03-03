#ifndef CHARACTER_FACTORY_H
#define CHARACTER_FACTORY_H



#include "Parser.h"
#include "Character.h"
#include <memory>



namespace Imperator {

class Character::Factory: commonItems::parser {
  public:
	explicit Factory();
	std::unique_ptr<Character> getCharacter(std::istream& theStream, const std::string& idString, const std::shared_ptr<GenesDB>& genesDB);

  private:
	std::unique_ptr<Character> character;
};

} // namespace Imperator



#endif // CHARACTER_FACTORY_H