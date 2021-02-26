#ifndef OUT_VERSION_H
#define OUT_VERSION_H



#include "Mappers/VersionParser/VersionParser.h"
#include <ostream>



namespace mappers
{
std::ostream& operator<<(std::ostream& output, const VersionParser& versionParser);
} // namespace mappers

#endif // OUT_VERSION_H