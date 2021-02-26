#ifndef OUT_TITLE_H
#define OUT_TITLE_H



#include "CK3/Titles/Title.h"
#include <ostream>



namespace CK3
{
std::ostream& operator<<(std::ostream& output, const Title& title);
} // namespace CK3


#endif // OUT_TITLE_H