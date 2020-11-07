#ifndef IMPERATOR_POP_H
#define IMPERATOR_POP_H

namespace Imperator
{
	class Pop
	{
	  public:
		class Factory;

		Pop() = default;

		unsigned long long ID = 0;
		std::string type;
		std::string culture;
		std::string religion;
	};
} // namespace Imperator

#endif // IMPERATOR_POP_H