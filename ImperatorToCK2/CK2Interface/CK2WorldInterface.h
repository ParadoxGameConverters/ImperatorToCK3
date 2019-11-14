#ifndef CK2_WORLD_INTERFACE
#define CK2_WORLD_INTERFACE



#include <string>



namespace CK2Interface
{

class World
{
	public:
		virtual ~World() {};
		virtual std::string getMessage() const = 0;
};

}



#endif // CK2_WORLD_INTERFACE
