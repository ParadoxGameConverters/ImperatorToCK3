using commonItems.Mods;
using ImperatorToCK3.Imperator;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator;

public class ImperatorDefinesTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private readonly ModFilesystem imperatorModFS = new(ImperatorRoot, System.Array.Empty<Mod>());

	[Fact]
	public void CohortSizeCanBeRead() {
		var defines = new ImperatorDefines();
		defines.LoadDefines(imperatorModFS);
		Assert.Equal(601, defines.CohortSize);
	}
}