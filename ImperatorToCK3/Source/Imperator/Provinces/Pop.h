#ifndef IMPERATOR_POP_H
#define IMPERATOR_POP_H



#include <string>



namespace Imperator {

class Pop {
public:
	class Factory;
	Pop() = default;

	[[nodiscard]] auto getID() const { return ID; }
	[[nodiscard]] const auto& getType() const { return type; }
	[[nodiscard]] const auto& getCulture() const { return culture; }
	[[nodiscard]] const auto& getReligion() const { return religion; }

private:
	unsigned long long ID = 0;
	std::string type;
	std::string culture;
	std::string religion;
};

} // namespace Imperator

#endif // IMPERATOR_POP_H