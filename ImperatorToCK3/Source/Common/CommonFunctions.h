#ifndef COMMON_FUNCTIONS_H
#define COMMON_FUNCTIONS_H

#include <string>

std::string trimPath(const std::string& fileName);
std::string trimExtension(const std::string& fileName);
std::string replaceCharacter(std::string fileName, char character);

#endif // COMMON_FUNCTIONS_H