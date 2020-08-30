#ifndef IMPERATOR_GENES_H
#define IMPERATOR_GENES_H

#include "AccessoryGenes.h"
#include "Parser.h"


namespace ImperatorWorld
{
	class GenesDB : commonItems::parser
	{
	public:
		GenesDB() = default;
		explicit GenesDB(const std::string& thePath);
		explicit GenesDB(std::istream& theStream);
		[[nodiscard]] const auto& getAccessoryGenes() const { return accessoryGenes; }

	private:
		void registerKeys();

		AccessoryGenes accessoryGenes;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_GENES_H
