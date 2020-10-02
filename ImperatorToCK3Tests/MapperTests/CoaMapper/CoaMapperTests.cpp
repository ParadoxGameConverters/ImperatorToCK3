#include "../ImperatorToCK3/Source/Mappers/CoaMapper/CoaMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_CoaMapperTests, getCoaForFlagNameReturnsCoaOnMatch)
{
	mappers::CoaMapper coaMapper("TestFiles/CoatsOfArms.txt");
	const auto* const coa1 = "= {pattern=\"pattern_solid.tga\"\ncolor1=ck2_green color2=bone_white color3=pitch_black colored_emblem={color1=bone_white color2=ck2_blue texture=\"ce_lamassu_01.dds\"\nmask={1 2 3 }instance={position={0.500000 0.500000 }scale={0.750000 0.750000 }depth=0.010000\nrotation=0\n}}colored_emblem={color1=bone_white color2=ck2_blue texture=\"ce_border_simple_02.tga\"\nmask={1 2 3 }instance={position={0.500000 0.500000 }scale={1.000000 1.000000 }depth=0.010000\nrotation=90\n}instance={position={0.500000 0.500000 }scale={1.000000 1.000000 }depth=0.010000\nrotation=270\n}}}";
	const auto* const coa2 = "= {pattern =\"pattern_solid.tga\"\ncolor1 =\"dark_green\"\ncolor2 =\"offwhite\"\ncolored_emblem ={texture =\"ce_pegasus_01.dds\"\ncolor1 =\"bone_white\"\ncolor2 =\"offwhite\"\ninstance ={scale ={-0.9 0.9 }}}colored_emblem ={texture =\"ce_border_simple_02.tga\"\ncolor1 =\"bone_white\"\ncolor2 =\"dark_green\"\ninstance ={rotation =0\nscale ={-1.0 1.0 }}instance ={rotation =180\nscale ={-1.0 1.0 }}}}";
	const auto* const coa3 = "= {pattern =\"pattern_solid.tga\"\ncolor1=\"offwhite\"\ncolor2=\"phrygia_red\"\ncolored_emblem ={texture =\"ce_knot_01.dds\"\ncolor1 =\"phrygia_red\"\ninstance ={scale ={0.75 0.75 }}}}";
	
	ASSERT_EQ(coa1, *coaMapper.getCoaForFlagName("e_IMPTOCK3_ADI"));
	ASSERT_EQ(coa2, *coaMapper.getCoaForFlagName("e_IMPTOCK3_AMK"));
	ASSERT_EQ(coa3, *coaMapper.getCoaForFlagName("e_IMPTOCK3_ANG"));
}

TEST(Mappers_CoaMapperTests, getCoaForFlagNameReturnsNulloptOnNonMatch)
{
	mappers::CoaMapper coaMapper("TestFiles/CoatsOfArms.txt");
	ASSERT_FALSE(coaMapper.getCoaForFlagName("e_IMPTOCK3_WRONG"));
}