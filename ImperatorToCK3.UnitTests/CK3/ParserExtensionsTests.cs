using commonItems;
using ImperatorToCK3.CK3;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3;

public class ParserExtensionsTests {
	[Theory]
	[InlineData(true, false, false, 0, 0)]
	[InlineData(false, true, false, 1, 1)]
	[InlineData(false, false, true, 2, 2)]
	[InlineData(true, true, false, 0, 1)]
	public void CorrectModDependentBranchesAreUsed(bool wtwsms, bool tfe, bool vanilla, int expectedValue1,
		int expectedValue2) {
		var ck3ModFlags = new Dictionary<string, bool> {["wtwsms"] = wtwsms, ["tfe"] = tfe, ["vanilla"] = vanilla,};

		var blocReader = new BufferedReader(
			"""
			MOD_DEPENDENT = {
				IF @wtwsms = { # Interpolated expression without brackets is valid, therefor should be supported.
					value1 = 0
				} ELSE_IF @[tfe] = {
					value1 = 1
				} ELSE = {
					value1 = 2
				}
				
				IF wtwsms = { # Simple mod flag string should be supported as well.
					value2 = 0
				} ELSE_IF @[vanilla] = {
					value2 = 2
				}
				IF @[tfe] = { # will override the previous value2 assignment
					value2 = 1
				}
				IF @[lotr] = { # Undefined mod flag in interpolated expression should resolve to false.
					value2 = 3
				}
				if @lotr = { # Undefined mod flag in interpolated expression without brackets should resolve to false.
					value2 = 4
				}
				IF lotr = { # Undefined mod flag string should resolve to false.
					value2 = 5
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