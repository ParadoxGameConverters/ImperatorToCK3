#ifndef IMPERATOR_ACCESSORY_GENES_H
#define IMPERATOR_ACCESSORY_GENES_H



#include "AccessoryGene.h"
#include "Parser.h"



namespace Imperator {

class AccessoryGenes : commonItems::parser {
public:
	AccessoryGenes() = default;
	explicit AccessoryGenes(std::istream& theStream);
		
	[[nodiscard]] auto getIndex() const { return index; }
	[[nodiscard]] const auto& getGenes() const { return genes; }

private:
	void registerKeys();

	unsigned int index = 0;
	std::map<std::string, AccessoryGene> genes;
};

} // namespace Imperator

#endif // IMPERATOR_ACCESSORY_GENES_H
