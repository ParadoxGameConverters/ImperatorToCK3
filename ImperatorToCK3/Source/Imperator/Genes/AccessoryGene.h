#ifndef IMPERATOR_ACCESSORY_GENE_H
#define IMPERATOR_ACCESSORY_GENE_H

#include "AccessoryGeneTemplate.h"
#include "Parser.h"

namespace ImperatorWorld
{
	class AccessoryGene : commonItems::parser
	{
	public:
		AccessoryGene() = default;
		explicit AccessoryGene(std::istream& theStream);

		[[nodiscard]] auto getIndex() const { return index; }
		[[nodiscard]] auto isInheritable() const { return inheritable; }
		[[nodiscard]] const auto& getGeneTemplates() const { return geneTemplates; }
		
	private:
		void registerKeys();

		int index = 0;
		bool inheritable = false;
		std::map<std::string, AccessoryGeneTemplate> geneTemplates;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_ACCESSORY_GENE_H
