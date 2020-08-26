#ifndef IMPERATOR_CHARACTER_PORTRAIT_DATA_H
#define IMPERATOR_CHARACTER_PORTRAIT_DATA_H



#include "Parser.h"



namespace ImperatorWorld
{

typedef struct CoordinatesStruct
{
	unsigned int x = 256; // palettes are 512x512
	unsigned int y = 256;
} CoordinatesStruct;


class CharacterPortraitData: commonItems::parser
{
	public:
	CharacterPortraitData() = default;
	explicit CharacterPortraitData(const std::string& dnaString);

	[[nodiscard]] const auto& getHairColorPaletteCoordinates() const { return hairColorPaletteCoordinates; }
	[[nodiscard]] const auto& getSkinColorPaletteCoordinates() const { return skinColorPaletteCoordinates; }
	[[nodiscard]] const auto& getEyeColorPaletteCoordinates() const { return eyeColorPaletteCoordinates; }

	private:
	CoordinatesStruct hairColorPaletteCoordinates;
	CoordinatesStruct skinColorPaletteCoordinates;
	CoordinatesStruct eyeColorPaletteCoordinates;
};

} // namespace ImperatorWorld



#endif // IMPERATOR_CHARACTER_PORTRAIT_DATA_H