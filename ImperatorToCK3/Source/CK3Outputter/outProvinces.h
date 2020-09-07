#ifndef CK3_OUT_PROVINCES
#define CK3_OUT_PROVINCES

#include <map>
#include <memory>
#include <string>
#include "../CK3/Province/CK3Province.h"


namespace CK3
{

	void outputHistoryProvinces(const std::string& outputModName, const std::map<int, std::shared_ptr<Province>>& provinces);

}



#endif // CK3_OUT_PROVINCES