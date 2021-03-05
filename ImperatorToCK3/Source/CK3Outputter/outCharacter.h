#ifndef OUT_CHARACTER_H
#define OUT_CHARACTER_H



#include <ostream>



namespace CK3 {

class Character;
std::ostream& operator<<(std::ostream& output, const Character& character);

} // namespace CK3



#endif // OUT_CHARACTER_H