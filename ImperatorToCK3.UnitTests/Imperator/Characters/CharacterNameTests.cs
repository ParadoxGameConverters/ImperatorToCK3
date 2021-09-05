using ImperatorToCK3.Imperator.Characters;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters {
	public class CharacterNameTests {
		[Fact] public void CustomNameOverridesName() {
			var reader = new BufferedReader("name=a custom_name=b");
			var characterName = new CharacterName(reader);
			Assert.Equal("b", characterName.Name);
		}
	}
}
