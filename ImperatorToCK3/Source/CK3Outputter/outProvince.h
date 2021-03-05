#ifndef OUT_PROVINCE
#define OUT_PROVINCE



#include <ostream>



namespace CK3 {

class Province;
std::ostream& operator<<(std::ostream& output, const Province& province);

} // namespace CK3



#endif // OUT_PROVINCE