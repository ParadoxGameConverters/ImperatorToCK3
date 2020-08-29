#ifndef IMPERATOR_GENES_H
#define IMPERATOR_GENES_H
#include "Gene.h"
#include "AccessoryGenes.h"
#include "Parser.h"


namespace ImperatorWorld
{
	class Gene;
	class GenesDB : commonItems::parser
	{
	public:
		GenesDB() = default;
		GenesDB(const std::string& thePath);
		GenesDB(std::istream& theStream);
		[[nodiscard]] const auto& getMorphGenes() const { return morphGenes; }
		[[nodiscard]] const auto& getAccessoryGenes() const { return accessoryGenes; }

		//void loadMorphGenes(std::istream& theStream);

	private:
		void registerKeys();

		std::map<std::string, Gene> morphGenes;
		AccessoryGenes accessoryGenes;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_GENES_H
