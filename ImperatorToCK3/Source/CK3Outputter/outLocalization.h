#ifndef CK3_OUT_LOCALIZATION_H
#define CK3_OUT_LOCALIZATION_H



#include "Configuration/Configuration.h"
#include <string>



namespace CK3 {

class World;
void outputLocalization(const std::string& imperatorPath, const std::string& outputName, const World& CK3World, const Configuration::IMPERATOR_DE_JURE& deJure);

}



#endif // CK3_OUT_LOCALIZATION_H