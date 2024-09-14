using commonItems;
using ImperatorToCK3.CK3;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ParserExtensionsTests {
	[Theory]
	[InlineData(true, false, false, 0, 0)]
	[InlineData(false, true, false, 1, 1)]
	[InlineData(false, false, true, 2, 0)]
	[InlineData(true, true, false, 0, 1)]
	public void CorrectModDependentBranchesAreUsed(bool wtwsms, bool tfe, bool vanilla, int expectedValue1,
		int expectedValue2) {
		var ck3ModFlags = new Dictionary<string, bool> {["wtwsms"] = wtwsms, ["tfe"] = tfe, ["vanilla"] = vanilla,};

		var blocReader = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				IF @wtwsms = { # Interpolated expression without brackets is valid, therefor should be supported.
					value1 = 0
				} ELSE_IF tfe = {# Simple mod flag string should be supported as well.
					value1 = 1
				} ELSE = {
					value1 = 2
				}
				
				IF @[wtwsms|vanilla|tfe] = { # Logical OR, example of more complex interpolated expression.
					value2 = 0
				}
				IF @[tfe] = { # Will override the previous value2.
					value2 = 1
				}
			}
			""");

		int? value1 = null;
		int? value2 = null;
		var parser = new Parser();
		parser.RegisterModDependentBloc(ck3ModFlags);
		parser.RegisterKeyword("value1", reader => value1 = reader.GetInt());
		parser.RegisterKeyword("value2", reader => value2 = reader.GetInt());
		parser.ParseStream(blocReader);

		Assert.Equal(expectedValue1, value1);
		Assert.Equal(expectedValue2, value2);
	}

	[Fact]
	public void ExceptionIsThrownWhenUnknownModFlagIsEncounteredInInterpolatedExpression() {
		var reader1 = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				IF @[unknown_mod] = { # Undefined mod flag in interpolated expression.
					value = 1
				} ELSE = {
					value = 2
				}
			}
			""");
		
		int? value = null;
		Dictionary<string, bool> modFlags = new();
		
		var parser1 = new Parser();
		parser1.RegisterModDependentBloc(modFlags);
		parser1.RegisterKeyword("value", reader => value = reader.GetInt());
		Assert.Throws<NCalc.Exceptions.NCalcParameterNotDefinedException>(() => parser1.ParseStream(reader1));
		Assert.Null(value);
		
		var reader2 = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				IF @unknown_mod = { # Undefined mod flag in interpolated expression without brackets.
					value = 3
				} ELSE = {
					value = 4
				}
			}
			""");
		var parser2 = new Parser();
		parser2.RegisterModDependentBloc(modFlags);
		parser2.RegisterKeyword("value", reader => value = reader.GetInt());
		Assert.Throws<KeyNotFoundException>(() => parser2.ParseStream(reader2));
		Assert.Null(value);
	}

	[Fact]
	public void ElseIfAfterElseIsIgnored() {
		Dictionary<string, bool> ck3ModFlags = new() {{"wtwsms", false}, {"tfe", true}, {"vanilla", false},};

		var blocReader = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				IF wtwsms = {
					value = 0
				} ELSE = {
					value = 2
				} ELSE_IF tfe = { # Should be ignored, even if the condition is true.
					value = 3
				}
			}
			""");

		int? value = null;
		var parser = new Parser();
		parser.RegisterModDependentBloc(ck3ModFlags);
		parser.RegisterKeyword("value", reader => value = reader.GetInt());
		parser.ParseStream(blocReader);

		Assert.Equal(2, value);
	}

	[Fact]
	public void ElseAfterElseIsIgnored() {
		Dictionary<string, bool> ck3ModFlags = new() {{"wtwsms", false}, {"tfe", true}, {"vanilla", false},};

		var blocReader = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				IF wtwsms = {
					value = 0
				} ELSE = {
					value = 2
				} ELSE = {
					value = 3
				}
			}
			""");

		int? value = null;
		var parser = new Parser();
		parser.RegisterModDependentBloc(ck3ModFlags);
		parser.RegisterKeyword("value", reader => value = reader.GetInt());
		parser.ParseStream(blocReader);

		Assert.Equal(2, value);
	}

	[Fact]
	public void ElseIfWithoutPrecedingIfIsIgnored() {
		Dictionary<string, bool> ck3ModFlags = new() {{"wtwsms", false}, {"tfe", true}, {"vanilla", false},};

		var blocReader = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				ELSE_IF tfe = { # Should be ignored, as there is no IF before it.
					value = 3
				}
			}
			""");

		int? value = null;
		var parser = new Parser();
		parser.RegisterModDependentBloc(ck3ModFlags);
		parser.RegisterKeyword("value", reader => value = reader.GetInt());
		parser.ParseStream(blocReader);

		Assert.Null(value);
	}

	[Fact]
	public void ElseWithoutPrecedingIfIsIgnored() {
		Dictionary<string, bool> ck3ModFlags = new() {{"wtwsms", false}, {"tfe", true}, {"vanilla", false},};

		var blocReader = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				ELSE = { # Should be ignored, as there is no IF before it.
					value = 3
				}
			}
			""");

		int? value = null;
		var parser = new Parser();
		parser.RegisterModDependentBloc(ck3ModFlags);
		parser.RegisterKeyword("value", reader => value = reader.GetInt());
		parser.ParseStream(blocReader);

		Assert.Null(value);
	}
}