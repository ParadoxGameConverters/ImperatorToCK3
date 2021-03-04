#ifndef CK3_OUT_CHARACTERS_H
#define CK3_OUT_CHARACTERS_H



#include <map>
#include <memory>
#include <string>



namespace CK3 {

class Character;
void outputCharacters(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Character>>& characters);

}



#endif // CK3_OUT_CHARACTERS_H