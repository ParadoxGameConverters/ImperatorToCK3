#ifndef OUT_TITLE_H
#define OUT_TITLE_H



#include <ostream>



namespace CK3 {

class Title;
std::ostream& operator<<(std::ostream& output, const Title& title);

} // namespace CK3



#endif // OUT_TITLE_H