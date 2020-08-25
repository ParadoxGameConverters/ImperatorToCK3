#include "PortraitData.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "base64.h"
#include <bitset>


long long binaryToDecimal(long long n)
{
	long long num = n;
	long long dec_value = 0;

	// Initializing base value to 1, i.e 2^0 
	int base = 1;

	long long temp = num;
	while (temp) {
		int last_digit = int(temp % 10);
		temp = temp / 10;

		dec_value += last_digit * long long(base);

		base = base * 2;
	}

	return dec_value;
}


ImperatorWorld::CharacterPortraitData::CharacterPortraitData(std::string theString)
{
	const std::string& decodedDnaStr = base64_decode(theString);

	std::string binary_outputInformations;
	for (auto i = 0; i < decodedDnaStr.size(); ++i)
	{
		std::bitset<8> b(decodedDnaStr.c_str()[i]);
		binary_outputInformations += b.to_string();
	}

	//hair
	hairColorPaletteCoordinates.x = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(0, 18))) / 512);
	hairColorPaletteCoordinates.y = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(0, 18))) % 512);
	//skin
	skinColorPaletteCoordinates.x = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(5, 18))) / 512);
	skinColorPaletteCoordinates.y = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(5, 18))) % 512);
	//eyes
	eyeColorPaletteCoordinates.x = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(10, 18))) / 512);
	eyeColorPaletteCoordinates.y = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(10, 18))) % 512);
}
