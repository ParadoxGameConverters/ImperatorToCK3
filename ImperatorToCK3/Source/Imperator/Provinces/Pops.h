#ifndef IMPERATOR_POPS_H
#define IMPERATOR_POPS_H



#include "PopFactory.h"
#include "Parser.h"



namespace Imperator {

class Pop;
class Pops : commonItems::parser {
public:
	void loadPops(std::istream& theStream);

	Pops& operator= (const Pops& obj) { this->pops = obj.pops; return *this; }

	[[nodiscard]] const auto& getPops() const { return pops; }

private:
	void registerKeys();

	Pop::Factory popFactory;

	std::map<unsigned long long, std::shared_ptr<Pop>> pops;
};


class PopsBloc : commonItems::parser {
public:
	explicit PopsBloc(std::istream& theStream);

	[[nodiscard]] const auto& getPopsFromBloc() const { return pops; }

private:
	void registerKeys();
		
	Pops pops;
};

} // namespace Imperator

#endif // IMPERATOR_POPS_H