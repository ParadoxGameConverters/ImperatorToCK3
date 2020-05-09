#ifndef IMPERATOR_FAMILIES_H
#define IMPERATOR_FAMILIES_H
#include "Parser.h"

namespace ImperatorWorld
{
	class Family;
	class Families : commonItems::parser
	{
	public:
		void loadFamiliesBloc(const std::string& thePath);
		void loadFamiliesBloc(std::istream& theStream);

		void loadFamilies(std::istream& theStream);

		[[nodiscard]] const auto& getFamilies() const { return families; }

	private:
		void registerBlocKeys();
		void registerKeys();

		std::map<int, std::shared_ptr<Family>> families;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_FAMILIES_H