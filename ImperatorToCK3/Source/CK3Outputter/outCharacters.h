#ifndef CK3_OUT_CHARACTERS
#define CK3_OUT_CHARACTERS

#include <map>
#include <memory>
#include <string>
#include "../CK3/Character/CK3Character.h"


namespace CK3
{
	void outputCharacters(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Character>>& characters);
}



#endif // CK3_OUT_CHARACTERS