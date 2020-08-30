#ifndef IMPERATOR_ACCESSORY_GENES_H
#define IMPERATOR_ACCESSORY_GENES_H
#include "AccessoryGene.h"
#include "Parser.h"


namespace ImperatorWorld
{
	class AccessoryGenes : commonItems::parser
	{
	public:
		AccessoryGenes() = default;
		AccessoryGenes(std::istream& theStream);
		
		[[nodiscard]] auto getIndex() const { return index; }
		[[nodiscard]] const auto& getGenes() const { return genes; }

	private:
		void registerKeys();

		int index = 0;
		std::map<std::string, AccessoryGene> genes;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_ACCESSORY_GENES_H
