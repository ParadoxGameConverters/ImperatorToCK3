#ifndef IMPERATOR_GENES_H
#define IMPERATOR_GENES_H
#include "AccessoryGene.h"
#include "AccessoryGenes.h"
#include "Parser.h"


namespace ImperatorWorld
{
	class AccessoryGene;
	class GenesDB : commonItems::parser
	{
	public:
		GenesDB() = default;
		GenesDB(const std::string& thePath);
		GenesDB(std::istream& theStream);
		//[[nodiscard]] const auto& getMorphGenes() const { return morphGenes; }
		[[nodiscard]] const auto& getAccessoryGenes() const { return accessoryGenes; }

	private:
		void registerKeys();

		//MorphGenes morphGenes;
		AccessoryGenes accessoryGenes;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_GENES_H
