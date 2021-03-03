#ifndef OUT_DYNASTY_H
#define OUT_DYNASTY_H



#include <ostream>



namespace CK3 {

class Dynasty;
std::ostream& operator<<(std::ostream& output, const Dynasty& dynasty);

}


#endif // OUT_DYNASTY_H