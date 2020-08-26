#include "PortraitData.h"
#include "base64.h"
#include <bitset>



ImperatorWorld::CharacterPortraitData::CharacterPortraitData(const std::string& dnaString)
{
	const auto decodedDnaStr = base64_decode(dnaString);

	//hair
	hairColorPaletteCoordinates.x = static_cast<uint8_t>(decodedDnaStr[0]) * 2;
	hairColorPaletteCoordinates.y = static_cast<uint8_t>(decodedDnaStr[1]) * 2;
	//skin
	skinColorPaletteCoordinates.x = static_cast<uint8_t>(decodedDnaStr[4]) * 2;
	skinColorPaletteCoordinates.y = static_cast<uint8_t>(decodedDnaStr[5]) * 2;
	//eyes
	eyeColorPaletteCoordinates.x = static_cast<uint8_t>(decodedDnaStr[8]) * 2;
	eyeColorPaletteCoordinates.y = static_cast<uint8_t>(decodedDnaStr[9]) * 2;
}