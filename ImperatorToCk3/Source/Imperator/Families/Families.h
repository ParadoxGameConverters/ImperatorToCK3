#ifndef IMPERATOR_FAMILIES_H
#define IMPERATOR_FAMILIES_H
#include "newParser.h"

namespace ImperatorWorld
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
} // namespace ImperatorWorld

#endif // IMPERATOR_FAMILIES_H