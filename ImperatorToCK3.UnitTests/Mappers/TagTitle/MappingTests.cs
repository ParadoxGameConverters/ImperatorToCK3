﻿using commonItems;
using ImperatorToCK3.Mappers.TagTitle;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle;

public class MappingTests {
	[Fact]
	public void SimpleTagMatch() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM }");
		var mapping = Mapping.Parse(reader);
		var match = mapping.RankMatch("ROM", "");

		Assert.Equal("e_roman_empire", match);
	}

	[Fact]
	public void SimpleTagMatchFailsOnWrongTag() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = REM }");
		var mapping = Mapping.Parse(reader);
		var match = mapping.RankMatch("ROM", "");

		Assert.Null(match);
	}

	[Fact]
	public void SimpleTagMatchFailsOnNoTag() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire }");
		var mapping = Mapping.Parse(reader);
		var match = mapping.RankMatch("ROM", "");

		Assert.Null(match);
	}

	[Fact]
	public void TagRankMatch() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM rank = e }");
		var mapping = Mapping.Parse(reader);
		var match = mapping.RankMatch("ROM", "e");

		Assert.Equal("e_roman_empire", match);
	}

	[Fact]
	public void TagRankMatchFailsOnWrongRank() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM rank = k }");
		var mapping = Mapping.Parse(reader);
		var match = mapping.RankMatch("ROM", "e");

		Assert.Null(match);
	}

	[Fact]
	public void TagRankMatchSucceedsOnNoRank() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM }");
		var mapping = Mapping.Parse(reader);
		var match = mapping.RankMatch("ROM", "e");

		Assert.Equal("e_roman_empire", match);
	}
}