#ifndef OUT_CHARACTER_H
#define OUT_CHARACTER_H

#include "../CK3/Character/CK3Character.h"
#include <ostream>

namespace CK3
{
std::ostream& operator<<(std::ostream& output, const Character& character);
} // namespace CK3


#endif // OUT_CHARACTER_H