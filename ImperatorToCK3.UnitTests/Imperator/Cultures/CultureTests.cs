using commonItems;
using ImperatorToCK3.Imperator.Cultures;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureTests {
	[Fact]
	public void CultureIsCorrectlyConstructed() {
		var reader = new BufferedReader("""
		{
			family={
				Python
				Obama
				Stroganov.Stroganova.Stroganovy.Stroganovian
				Romanov.Romanova.Romanovy.Romanovian
			}
		}
		""");

		var culture = new Culture("balkan", reader);

		Assert.Equal("balkan", culture.Id);
		Assert.Equal("Python", culture.GetMaleFamilyNameForm("Python"));
		Assert.Equal("Obama", culture.GetMaleFamilyNameForm("Obama"));
		Assert.Equal("Stroganov", culture.GetMaleFamilyNameForm("Stroganovy"));
		Assert.Equal("Romanov", culture.GetMaleFamilyNameForm("Romanovy"));
	}

	[Fact]
	public void WarningIsLoggedWhenFamilyNameHasIncorrectFormat() {
		var reader = new BufferedReader("""
		{
			family={
				Stroganov.Stroganova.Stroganovy
			}
		}
		""");

		var writer = new StringWriter();
		Console.SetOut(writer);
		var culture = new Culture("balkan", reader);

		Assert.Contains("[WARN] Unknown family name format: Stroganov.Stroganova.Stroganovy", writer.ToString());
		Assert.Null(culture.GetMaleFamilyNameForm("Stroganovy"));
	}
}