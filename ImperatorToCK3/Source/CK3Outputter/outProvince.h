#ifndef OUT_PROVINCE
#define OUT_PROVINCE

#include "../CK3/Province/CK3Province.h"
#include <ostream>

namespace CK3
{
std::ostream& operator<<(std::ostream& output, const Province& province);
} // namespace CK3


#endif // OUT_PROVINCE