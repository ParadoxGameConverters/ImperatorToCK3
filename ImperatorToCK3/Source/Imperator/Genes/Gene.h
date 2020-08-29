#ifndef IMPERATOR_GENE_H
#define IMPERATOR_GENE_H

#include "GeneTemplate.h"
#include "Parser.h"

namespace ImperatorWorld
{
	class Gene : commonItems::parser
	{
	public:
		Gene() = default;
		explicit Gene(std::istream& theStream, const std::string& geneTypeStr = "accessory_gene");

		[[nodiscard]] const auto& getIndex() const { return index; }


	private:
		void registerKeys();

		std::string geneType;
		int index = 0;
		std::unordered_map<std::string, GeneTemplate> geneTemplates;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_GENE_H
