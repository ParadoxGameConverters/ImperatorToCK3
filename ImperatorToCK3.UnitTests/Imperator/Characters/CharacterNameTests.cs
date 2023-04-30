using commonItems;
using ImperatorToCK3.Imperator.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters; 

public class CharacterNameTests {
	[Fact]
	public void NameAndCustomNameDefaultToCorrectValues() {
		var reader = new BufferedReader(string.Empty);
		var characterName = new CharacterName(reader);
		Assert.Equal(string.Empty, characterName.Name);
		Assert.Null(characterName.CustomName);
	}
	[Fact]
	public void NameAndCustomNameCanBeRead() {
		var reader = new BufferedReader("name=a custom_name=b");
		var characterName = new CharacterName(reader);
		Assert.Equal("a", characterName.Name);
		Assert.Equal("b", characterName.CustomName);
	}
}