#ifndef IMPERATOR_ACCESSORYGENES_H
#define IMPERATOR_ACCESSORYGENES_H
#include "Gene.h"
#include "Parser.h"


namespace ImperatorWorld
{
	class Gene;
	class AccessoryGenes : commonItems::parser
	{
	public:
		AccessoryGenes() = default;
		AccessoryGenes(std::istream& theStream);
		[[nodiscard]] const auto& getGenes() const { return genes; }
		[[nodiscard]] const auto& getIndex() const { return index; }


	private:
		void registerKeys();

		int index = 0;
		std::map<std::string, Gene> genes;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_ACCESSORYGENES_H
