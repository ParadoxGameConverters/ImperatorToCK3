#ifndef IMPERATOR_POPS_H
#define IMPERATOR_POPS_H
#include "Parser.h"

namespace Imperator
{
	class Pop;
	class Pops : commonItems::parser
	{
	public:
		void loadPops(std::istream& theStream);

		[[nodiscard]] const auto& getPops() const { return pops; }

	private:
		void registerKeys();

		std::map<int, std::shared_ptr<Pop>> pops;
	};


	class PopsBloc : commonItems::parser
	{
	public:
		PopsBloc() = default;
		explicit PopsBloc(std::istream & theStream);

		[[nodiscard]] const auto& getPopsFromBloc() const { return pops; }

	private:
		void registerKeys();

		Pops pops;
	};
} // namespace Imperator

#endif // IMPERATOR_POPS_H