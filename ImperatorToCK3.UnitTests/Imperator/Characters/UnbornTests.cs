using commonItems;
using ImperatorToCK3.Imperator.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters;

public class UnbornTests {
	[Fact]
	public void UnbornNeedsMotherAndFatherAndBirthDate() {
		var reader = new BufferedReader("father=2 date=90.9.10");
		Assert.Null(Unborn.Parse(reader));
		reader = new BufferedReader("mother=1 date=90.9.10");
		Assert.Null(Unborn.Parse(reader));
		reader = new BufferedReader("mother=1 father=2");
		Assert.Null(Unborn.Parse(reader));

		reader = new BufferedReader("mother=1 father=2 date=90.9.10");
		Assert.NotNull(Unborn.Parse(reader));
	}

	[Fact]
	public void EstimatedConceptionDateIs280DaysBeforeBirthDate() {
		var reader = new BufferedReader("mother=1 father=2 date=990.10.8");
		var unborn = Unborn.Parse(reader);
		Assert.NotNull(unborn);

		Assert.Equal(new Date(237,10,8), unborn.BirthDate);
		Assert.Equal(new Date(237,1,1), unborn.EstimatedConceptionDate);
		Assert.Equal(unborn.EstimatedConceptionDate, unborn.BirthDate.ChangeByDays(-280));
	}
}