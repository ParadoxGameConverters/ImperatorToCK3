#include "PortraitData.h"
#include "../Genes/GenesDB.h"
#include "Log.h"
#include "base64.h"
#include <bitset>
#include <utility>



Imperator::CharacterPortraitData::CharacterPortraitData(const std::string& dnaString, const std::shared_ptr<GenesDB>& genesDB, const std::string& ageSexString) : genes(genesDB)
{
	const auto decodedDnaStr = base64_decode(dnaString);

	//hair
	hairColorPaletteCoordinates.x = static_cast<uint8_t>(decodedDnaStr.at(0)) * 2;
	hairColorPaletteCoordinates.y = static_cast<uint8_t>(decodedDnaStr.at(1)) * 2;
	//skin
	skinColorPaletteCoordinates.x = static_cast<uint8_t>(decodedDnaStr.at(4)) * 2;
	skinColorPaletteCoordinates.y = static_cast<uint8_t>(decodedDnaStr.at(5)) * 2;
	//eyes
	eyeColorPaletteCoordinates.x = static_cast<uint8_t>(decodedDnaStr.at(8)) * 2;
	eyeColorPaletteCoordinates.y = static_cast<uint8_t>(decodedDnaStr.at(9)) * 2;

	//accessory genes
	const unsigned int colorGenesBytes = 12;

	auto accessoryGenes = genes->getAccessoryGenes().getGenes();

	//LOG(LogLevel::Debug) << "ageSex: " << ageSexString;
	const auto accessoryGenesIndex = genes->getAccessoryGenes().getIndex();
	for (auto& [geneName, gene] : accessoryGenes)
	{
		const auto geneIndex = gene.getIndex();
		//Log(LogLevel::Debug) << "\tgene: " << geneItr.first;
		
		const auto geneTemplateByteIndex = colorGenesBytes + (accessoryGenesIndex + geneIndex - 3) * 4;
		const auto characterGeneTemplateIndex = static_cast<uint8_t>(decodedDnaStr.at(geneTemplateByteIndex));
		const auto& [fst, snd] = gene.getGeneTemplateByIndex(characterGeneTemplateIndex);
		//Log(LogLevel::Debug) << "\t\tgene template: " << fst;
		
		const auto geneTemplateObjectByteIndex = colorGenesBytes + (accessoryGenesIndex + geneIndex - 3) * 4 + 1;
		const auto characterGeneSliderValue = static_cast<uint8_t>(decodedDnaStr.at(geneTemplateObjectByteIndex)) / 255;
		auto characterGeneFoundWeightBlock = gene.getGeneTemplates().find(fst)->second.getAgeSexWeightBlocs().find(ageSexString);
		if (characterGeneFoundWeightBlock != gene.getGeneTemplates().find(fst)->second.getAgeSexWeightBlocs().end())
		{
			auto characterGeneObjectName = characterGeneFoundWeightBlock->second->getMatchingObject(characterGeneSliderValue);
			if (characterGeneObjectName)
			{
				//Log(LogLevel::Debug) << "\t\tgene template object: " << characterGeneObjectName.value();
				accessoryGenesVector.emplace_back(AccessoryGeneStruct{ geneName, fst, characterGeneObjectName.value() });
				//Log(LogLevel::Debug) << "\t\tStruct size: " << accessoryGenesVector.size();
			}
			else Log(LogLevel::Warning) << "\t\t\tgene template object name could not be extracted from DNA";
		}
	}
}