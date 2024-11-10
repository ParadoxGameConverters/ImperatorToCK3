using commonItems;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Jobs;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Jobs;

public class OfficeJobTests {
	private static readonly string officeJobString = 
		"""
            {
                who=1770
                character=207696
                start_date=1580.5.26
                office="office_physician"
            }                   
        """;
	
	[Fact]
	public void OfficeJobCanBeLoaded() {
		var reader = new BufferedReader(officeJobString);
		var irCharacters = new CharacterCollection {new Character(207696)};

		var officeJob = new OfficeJob(reader, irCharacters);
		Assert.Equal((ulong)1770, officeJob.CountryId);
		Assert.Equal((ulong)207696, officeJob.Character.Id);
		Assert.Equal(new Date(827, 5, 26), officeJob.StartDate); // The original date is AUC.
		Assert.Equal("office_physician", officeJob.OfficeType);
	}

	[Fact]
	public void FormatExceptionIsThrownOnMissingCountryId() {
		var officeJobStr = officeJobString.Replace("who=1770", "");
		
		var reader = new BufferedReader(officeJobStr);
		var irCharacters = new CharacterCollection {new Character(207696)};

		var exception = Assert.Throws<FormatException>(() => new OfficeJob(reader, irCharacters));
		Assert.Equal("Country ID missing!", exception.Message);
	}
	
	[Fact]
	public void FormatExceptionIsThrownOnMissingCharacterId() {
		var officeJobStr = officeJobString.Replace("character=207696", "");
		
		var reader = new BufferedReader(officeJobStr);
		var irCharacters = new CharacterCollection();

		var exception = Assert.Throws<FormatException>(() => new OfficeJob(reader, irCharacters));
		Assert.Equal("Character ID missing!", exception.Message);
	}
	
	[Fact]
	public void FormatExceptionIsThrownOnMissingOfficeType() {
		var officeJobStr = officeJobString.Replace("office=\"office_physician\"", "");
		
		var reader = new BufferedReader(officeJobStr);
		var irCharacters = new CharacterCollection {new Character(207696)};

		var exception = Assert.Throws<FormatException>(() => new OfficeJob(reader, irCharacters));
		Assert.Equal("Office type missing!", exception.Message);
	}
}