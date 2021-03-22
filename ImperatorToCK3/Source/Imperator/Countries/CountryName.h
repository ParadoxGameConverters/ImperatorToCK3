#ifndef IMPERATOR_COUNTRY_NAME_H
#define IMPERATOR_COUNTRY_NAME_H



#include <string>
#include <optional>
#include <memory>



namespace Imperator {

class CountryName {
public:
	CountryName() = default;
	CountryName(const CountryName& other) : name(other.name), adjective(other.adjective) {
		memcpy(&base, &other.base, sizeof std::unique_ptr<CountryName>);
	}
	class Factory;
	
	friend void swap(CountryName& first, CountryName& second) // nothrow
	{
		using std::swap;

		// by swapping the members of two objects,
		// the two objects are effectively swapped
		swap(first.name, second.name);
		swap(first.adjective, second.adjective);
		swap(first.base, second.base);
	}
	
	auto& operator=(const CountryName& other) noexcept {
		CountryName local(other);
		swap(*this, local);
		return *this;
	}

	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] std::string getAdjective() const;
	[[nodiscard]] const auto& getBase() const { return base; }

private:
	std::string name;
	std::optional<std::string> adjective;
	std::unique_ptr<CountryName> base;
};

} // namespace Imperator



#endif // IMPERATOR_COUNTRY_NAME_H