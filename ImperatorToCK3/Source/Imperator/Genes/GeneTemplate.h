#ifndef IMPERATOR_GENE_TEMPLATE_H
#define IMPERATOR_GENE_TEMPLATE_H

#include "Parser.h"
#include "WeightBloc.h"

namespace ImperatorWorld
{
	class WeightBloc;
	class GeneTemplate : commonItems::parser
	{
	public:
		GeneTemplate() = default;
		explicit GeneTemplate(std::istream& theStream, const std::string& geneTypeStr = "accessory_gene");

		[[nodiscard]] const auto& getIndex() const { return index; }


	private:
		void registerKeys();

		std::string geneType;
		int index = 0;
		std::map<std::string, std::shared_ptr<WeightBloc>> ageSexWeightBlocs;
		//std::map<std::string, std::shared_ptr<SettingsBloc>> ageSexSettingsBlocs;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_GENE_TEMPLATE_H
