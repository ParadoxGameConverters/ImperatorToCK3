#ifndef IMPERATOR_CHARACTER_PORTRAIT_DATA_H
#define IMPERATOR_CHARACTER_PORTRAIT_DATA_H


#include "Parser.h"
#include "../Genes/GenesDB.h"


namespace ImperatorWorld
{

typedef struct CoordinatesStruct
{
	unsigned int x = 256; // palettes are 512x512
	unsigned int y = 256;
} CoordinatesStruct;

typedef struct AccessoryGeneStruct
{
	std::string geneName;
	std::string geneTemplate;
	std::string objectName;
} AccessoryGeneStruct;


class CharacterPortraitData: commonItems::parser
{
public:
	CharacterPortraitData() = default;
	explicit CharacterPortraitData(const std::string& dnaString, const GenesDB& genesDB, const std::string& ageSexString = "male");

	[[nodiscard]] const auto& getHairColorPaletteCoordinates() const { return hairColorPaletteCoordinates; }
	[[nodiscard]] const auto& getSkinColorPaletteCoordinates() const { return skinColorPaletteCoordinates; }
	[[nodiscard]] const auto& getEyeColorPaletteCoordinates() const { return eyeColorPaletteCoordinates; }
	[[nodiscard]] const auto& getAccessoryGenesVector() const { return accessoryGenesVector; }

private:
	CoordinatesStruct hairColorPaletteCoordinates;
	CoordinatesStruct skinColorPaletteCoordinates;
	CoordinatesStruct eyeColorPaletteCoordinates;
	GenesDB genes;
	std::vector<AccessoryGeneStruct> accessoryGenesVector;
};

} // namespace ImperatorWorld



#endif // IMPERATOR_CHARACTER_PORTRAIT_DATA_H