#ifndef IMPERATOR_ACCESSORY_GENE_TEMPLATE_H
#define IMPERATOR_ACCESSORY_GENE_TEMPLATE_H



#include "Parser.h"



namespace Imperator {

class WeightBlock;
class AccessoryGeneTemplate : commonItems::parser {
public:
	AccessoryGeneTemplate() = default;
	explicit AccessoryGeneTemplate(std::istream& theStream);

	[[nodiscard]] auto getIndex() const { return index; }
	[[nodiscard]] const auto& getAgeSexWeightBlocs() const { return ageSexWeightBlocks; }

private:
	void registerKeys();

	unsigned int index = 0;
	std::map<std::string, std::shared_ptr<WeightBlock>> ageSexWeightBlocks;
};

} // namespace Imperator

#endif // IMPERATOR_ACCESSORY_GENE_TEMPLATE_H
