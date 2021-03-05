#ifndef OUT_VERSION_H
#define OUT_VERSION_H



#include <ostream>



namespace mappers {

class VersionParser;
std::ostream& operator<<(std::ostream& output, const VersionParser& versionParser);

} // namespace mappers



#endif // OUT_VERSION_H