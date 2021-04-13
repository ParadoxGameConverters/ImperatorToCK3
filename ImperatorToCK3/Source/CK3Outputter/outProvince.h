#ifndef OUT_PROVINCE_H
#define OUT_PROVINCE_H



#include <ostream>



namespace CK3 {

class Province;
std::ostream& operator<<(std::ostream& output, const Province& province);

} // namespace CK3



#endif // OUT_PROVINCE_H