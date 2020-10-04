#include "../ImperatorToCK3/Source/Mappers/CoaMapper/CoaMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_CoaMapperTests, getCoaForFlagNameReturnsCoaOnMatch)
{
	mappers::CoaMapper coaMapper("TestFiles/CoatsOfArms.txt");
	const auto* const coa1 = R"(= {
    pattern="pattern_solid.tga"
    color1=ck2_green    color2=bone_white   color3=pitch_black  colored_emblem={
        color1=bone_white       color2=ck2_blue     texture="ce_lamassu_01.dds"
        mask={ 1 2 3 }
        instance={
            position={ 0.500000 0.500000 }
            scale={ 0.750000 0.750000 }
            depth=0.010000
            rotation=0
        }
    }
    colored_emblem={
        color1=bone_white       color2=ck2_blue     texture="ce_border_simple_02.tga"
        mask={ 1 2 3 }
        instance={
            position={ 0.500000 0.500000 }
            scale={ 1.000000 1.000000 }
            depth=0.010000
            rotation=90
        }
        instance={
            position={ 0.500000 0.500000 }
            scale={ 1.000000 1.000000 }
            depth=0.010000
            rotation=270
        }
    }
})";
	const auto* const coa2 = "= {\n"
  "\tpattern =\"pattern_solid.tga\"\n"
  "\tcolor1 =\"dark_green\"\n"
  "\tcolor2 =\"offwhite\"\n"
  "\tcolored_emblem ={\n"
  "\t\ttexture =\"ce_pegasus_01.dds\"\n"
  "\t\tcolor1 =\"bone_white\"\n"
  "\t\tcolor2 =\"offwhite\"\n"
  "\t\tinstance ={\n"
  "\t\t\tscale ={-0.9 0.9 }\"\n"
  "\t\t}\n"
  "\t}\n"
  "\tcolored_emblem ={\n"
  "\t\ttexture =\"ce_border_simple_02.tga\"\n"
  "\t\tcolor1 =\"bone_white\"\n"
  "\t\tcolor2 =\"dark_green\"\n"
  "\t\tinstance ={\n"
  "\t\t\trotation =0\n"
  "\t\t\tscale ={-1.0 1.0 }\n"
  "\t\t}\n"
  "\t\tinstance ={\n"
  "\t\t\trotation =180\n"
  "\t\t\tscale ={-1.0 1.0 }\n"
  "\t\t}\n"
  "\t}\n"
  "}";
	const auto* const coa3 = R"(= {
	pattern ="pattern_solid.tga"
	color1="offwhite"
	color2="phrygia_red"
	colored_emblem ={
		texture ="ce_knot_01.dds"
		color1 ="phrygia_red"
		instance ={
			scale ={0.75 0.75 }
		}
	}
})";
	
	ASSERT_EQ(coa1, *coaMapper.getCoaForFlagName("e_IMPTOCK3_ADI"));
	ASSERT_EQ(coa2, *coaMapper.getCoaForFlagName("e_IMPTOCK3_AMK"));
	ASSERT_EQ(coa3, *coaMapper.getCoaForFlagName("e_IMPTOCK3_ANG"));
}

TEST(Mappers_CoaMapperTests, getCoaForFlagNameReturnsNulloptOnNonMatch)
{
	mappers::CoaMapper coaMapper("TestFiles/CoatsOfArms.txt");
	ASSERT_FALSE(coaMapper.getCoaForFlagName("e_IMPTOCK3_WRONG"));
}