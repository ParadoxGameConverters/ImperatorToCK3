using commonItems;
using commonItems.Serialization;
using FluentAssertions;
using ImperatorToCK3.CK3.Religions;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class FaithTests {
	[Fact]
	public void HolySitesAreLoaded() {
		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("chalcedonian", reader);
		
		Assert.Collection(faith.HolySiteIds,
			site=>Assert.Equal("rome", site),
			site=>Assert.Equal("constantinople", site),
			site=>Assert.Equal("antioch", site));
	}

	[Fact]
	public void FaithAttributesAreReadAndSerialized() {
		var reader = new BufferedReader(@"{
			icon = celtic_pagan
			doctrine = tenet_esotericism
			doctrine = tenet_human_sacrifice # should not replace the line above
		}");
		var faith = new Faith("celtic_pagan", reader);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().ContainAll(
			"icon=celtic_pagan",
			"doctrine=tenet_esotericism",
			"doctrine=tenet_human_sacrifice"
		);
	}

	[Fact]
	public void HolySiteIdCanBeReplaced() {
		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("orthodox", reader);
		Assert.False(faith.ModifiedByConverter);
		
		faith.ReplaceHolySiteId("antioch", "jerusalem");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "jerusalem");
		Assert.True(faith.ModifiedByConverter);
	}

	[Fact]
	public void ReplacingMissingHolySiteIdDoesNotChangeHolySites() {
		var output = new StringWriter();
		Console.SetOut(output);
		
		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("orthodox", reader);
		Assert.False(faith.ModifiedByConverter);
		
		faith.ReplaceHolySiteId("washington", "jerusalem");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch");
		Assert.False(faith.ModifiedByConverter);
		Assert.Contains("washington does not belong to holy sites of faith orthodox and cannot be replaced!", output.ToString());
	}
}