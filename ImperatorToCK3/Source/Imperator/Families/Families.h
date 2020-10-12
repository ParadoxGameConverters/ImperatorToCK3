#ifndef IMPERATOR_FAMILIES_H
#define IMPERATOR_FAMILIES_H
#include "Parser.h"

namespace Imperator
{
	class Family;
	class Families : commonItems::parser
	{
	public:
		void loadFamilies(const std::string& thePath);
		void loadFamilies(std::istream& theStream);

		[[nodiscard]] const auto& getFamilies() const { return families; }

	private:
		void registerKeys();

		std::map<int, std::shared_ptr<Family>> families;
	};

	class FamiliesBloc : commonItems::parser
	{
	public:
		FamiliesBloc() = default;
		explicit FamiliesBloc(std::istream & theStream);

		[[nodiscard]] const auto& getFamiliesFromBloc() const { return families; }

	private:
		void registerKeys();

		Families families;
	};
} // namespace Imperator

#endif // IMPERATOR_FAMILIES_H