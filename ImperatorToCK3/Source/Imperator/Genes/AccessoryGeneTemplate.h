#ifndef IMPERATOR_ACCESSORY_GENE_TEMPLATE_H
#define IMPERATOR_ACCESSORY_GENE_TEMPLATE_H

#include "Parser.h"
#include "WeightBlock.h"

namespace ImperatorWorld
{
	class AccessoryGeneTemplate : commonItems::parser
	{
	public:
		AccessoryGeneTemplate() = default;
		explicit AccessoryGeneTemplate(std::istream& theStream);

		[[nodiscard]] auto getIndex() const { return index; }
		[[nodiscard]] const auto& getAgeSexWeightBlocs() const { return ageSexWeightBlocks; }

	private:
		void registerKeys();

		int index = 0;
		std::map<std::string, std::shared_ptr<WeightBlock>> ageSexWeightBlocks;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_ACCESSORY_GENE_TEMPLATE_H
