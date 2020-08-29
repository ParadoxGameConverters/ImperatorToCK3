#ifndef IMPERATOR_ACCESSORY_GENE_TEMPLATE_H
#define IMPERATOR_ACCESSORY_GENE_TEMPLATE_H

#include "Parser.h"
#include "WeightBloc.h"

namespace ImperatorWorld
{
	class WeightBloc;
	class AccessoryGeneTemplate : commonItems::parser
	{
	public:
		AccessoryGeneTemplate() = default;
		explicit AccessoryGeneTemplate(std::istream& theStream);

		[[nodiscard]] const auto& getIndex() const { return index; }
		[[nodiscard]] const auto& getAgeSexWeightBlocs() const { return ageSexWeightBlocs; }


	private:
		void registerKeys();

		int index = 0;
		std::map<std::string, std::shared_ptr<WeightBloc>> ageSexWeightBlocs;
		//std::map<std::string, std::shared_ptr<SettingsBloc>> ageSexSettingsBlocs; // TODO: move to MorphGeneTemplate class when it's added
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_ACCESSORY_GENE_TEMPLATE_H
