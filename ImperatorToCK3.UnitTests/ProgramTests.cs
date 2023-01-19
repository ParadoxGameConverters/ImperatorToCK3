using System;
using System.Globalization;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProgramTests {
	[Fact]
	public void CultureIsSetToInvariantCulture() {
		Program.Main(Array.Empty<string>());
		
		Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
		Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
		Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.DefaultThreadCurrentCulture);
		Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.DefaultThreadCurrentUICulture);
	}

	[Fact]
	public void WarningIsLoggedWhenParametersArePassed() {
		var output = new StringWriter();
		Console.SetOut(output);

		Program.Main(new[] {"--debug"});

		var outStr = output.ToString();
		Assert.Contains("[WARN] ImperatorToCK3 takes no parameters.\n" +
		                "It uses configuration.txt, configured manually or by the frontend.", outStr);
	}
}