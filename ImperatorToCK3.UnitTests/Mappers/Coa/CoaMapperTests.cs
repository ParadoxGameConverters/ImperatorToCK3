using commonItems.Mods;
using ImperatorToCK3.Mappers.CoA;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Coa;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public sealed class CoaMapperTests {
	private const string ImperatorRoot = "TestFiles/MapperTests/CoaMapper/Imperator/game";
	private static readonly ModFilesystem imperatorModFs = new(ImperatorRoot, Array.Empty<Mod>());

	[Fact]
	public void GetCoaForFlagNameReturnsCoaOnMatch() {
		var coaMapper = new CoaMapper(imperatorModFs);
		// ReSharper disable once StringLiteralTypo
		const string coa1 = @"{
                pattern=""pattern_solid.tga""
                color1=ck2_green    color2=bone_white   color3=pitch_black  colored_emblem={
                    color1=bone_white       color2=ck2_blue     texture=""ce_lamassu_01.dds""
                    mask ={ 1 2 3 }
                    instance={
                        position={ 0.500000 0.500000 }
                        scale={ 0.750000 0.750000 }
                        depth=0.010000
                        rotation=0
                    }
                }
                colored_emblem={
                    color1=bone_white       color2=ck2_blue     texture=""ce_border_simple_02.tga""
                    mask ={ 1 2 3 }
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
            }";
		const string coa2 = "{\n" +
		                    "\tpattern =\"pattern_solid.tga\"\n" +
		                    "\tcolor1 =\"dark_green\"\n" +
		                    "\tcolor2 =\"offwhite\"\n" +
		                    "\tcolored_emblem ={\n" +
		                    "\t\ttexture =\"ce_pegasus_01.dds\"\n" +
		                    "\t\tcolor1 =\"bone_white\"\n" +
		                    "\t\tcolor2 =\"offwhite\"\n" +
		                    "\t\tinstance ={\n" +
		                    "\t\t\tscale ={-0.9 0.9 }\n" +
		                    "\t\t}\n" +
		                    "\t}\n" +
		                    "\tcolored_emblem ={\n" +
		                    "\t\ttexture =\"ce_border_simple_02.tga\"\n" +
		                    "\t\tcolor1 =\"bone_white\"\n" +
		                    "\t\tcolor2 =\"dark_green\"\n" +
		                    "\t\tinstance ={\n" +
		                    "\t\t\trotation =0\n" +
		                    "\t\t\tscale ={-1.0 1.0 }\n" +
		                    "\t\t}\n" +
		                    "\t\tinstance ={\n" +
		                    "\t\t\trotation =180\n" +
		                    "\t\t\tscale ={-1.0 1.0 }\n" +
		                    "\t\t}\n" +
		                    "\t}\n" +
		                    "}";
		const string coa3 = @"{
	            pattern =""pattern_solid.tga""
	            color1=""offwhite""
                color2 =""phrygia_red""
                colored_emblem ={
		            texture =""ce_knot_01.dds""
                    color1 =""phrygia_red""
                    instance ={
			            scale ={0.75 0.75 }
		            }
	            }
            }";
		Assert.Equal(coa1.Split('\n').Length,
			coaMapper.GetCoaForFlagName("e_IRTOCK3_ADI", warnIfMissing: false)!.Split('\n').Length);
		Assert.Equal(coa2.Split('\n').Length,
			coaMapper.GetCoaForFlagName("e_IRTOCK3_AMK", warnIfMissing: false)!.Split('\n').Length);
		Assert.Equal(coa3.Split('\n').Length,
			coaMapper.GetCoaForFlagName("e_IRTOCK3_ANG", warnIfMissing: false)!.Split('\n').Length);
	}

	[Fact]
	public void GetCoaForFlagNameReturnsNullOnNonMatch() {
		var coaMapper = new CoaMapper(imperatorModFs);
		Assert.Null(coaMapper.GetCoaForFlagName("e_IRTOCK3_WRONG", warnIfMissing: false));
	}

	[Fact]
	public void ParseCoAsParsesTemplatesAndCoas() {
		var coaMapper = new CoaMapper();

		coaMapper.ParseCoAs([
			"""
			template = {
				some_template = {
					pattern = "pattern_solid.tga"
				}
			}
			""",
			"""
			e_IRTOCK3_TEST = {
				pattern = "pattern_solid.tga"
				color1 = red
			}
			"""
		]);

		// The template should be ignored, while the CoA should be parsed.
		Assert.Null(coaMapper.GetCoaForFlagName("some_template", warnIfMissing: false));

		var coa = coaMapper.GetCoaForFlagName("e_IRTOCK3_TEST", warnIfMissing: false);
		Assert.NotNull(coa);
		Assert.Contains("pattern_solid.tga", coa);

		// Variables are handled implicitly by the parser, so none should be stored for output.
		Assert.Empty(coaMapper.VariablesToOutput);
	}

	[Fact]
	public void ParseCoAsSupportsMultipleDefinitionsInOneString() {
		var coaMapper = new CoaMapper();

		coaMapper.ParseCoAs([
			"""
			e_IRTOCK3_FIRST = { pattern = "first.tga" }
			e_IRTOCK3_SECOND = { pattern = "second.tga" }
			"""
		]);

		Assert.Contains("first.tga", coaMapper.GetCoaForFlagName("e_IRTOCK3_FIRST", warnIfMissing: false));
		Assert.Contains("second.tga", coaMapper.GetCoaForFlagName("e_IRTOCK3_SECOND", warnIfMissing: false));
	}
}