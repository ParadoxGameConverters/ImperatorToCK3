using commonItems;
using ImperatorToCK3.Imperator.Cultures;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureGroupTests {
	[Fact]
	public void CultureGroupIsCorrectlyConstructedWithIdOnly() {
		var reader = new BufferedReader("{}");

		var cultureGroup = new CultureGroup("test_group", reader);

		Assert.Equal("test_group", cultureGroup.Id);
		Assert.Empty(cultureGroup);
	}

	[Fact]
	public void CultureGroupCorrectlyParsesCultures() {
		var reader = new BufferedReader("""
		{
			culture = {
				roman = {
					family = {
						Julius
						Caesar
					}
				}
				greek = {
					family = {
						Alexander
						Aristotle
					}
				}
			}
		}
		""");

		var cultureGroup = new CultureGroup("classical", reader);

		Assert.Equal("classical", cultureGroup.Id);
		Assert.Equal(2, cultureGroup.Count);
		Assert.NotNull(cultureGroup["roman"]);
		Assert.NotNull(cultureGroup["greek"]);
		Assert.Equal("roman", cultureGroup["roman"].Id);
		Assert.Equal("greek", cultureGroup["greek"].Id);
	}

	[Fact]
	public void CultureGroupCorrectlyParsesFamilyNamesWithSimpleFormat() {
		var reader = new BufferedReader("""
		{
			family = {
				Smith
				Johnson
				Williams
			}
		}
		""");

		var cultureGroup = new CultureGroup("anglo_saxon", reader);

		Assert.Equal("Smith", cultureGroup.GetMaleFamilyNameForm("Smith"));
		Assert.Equal("Johnson", cultureGroup.GetMaleFamilyNameForm("Johnson"));
		Assert.Equal("Williams", cultureGroup.GetMaleFamilyNameForm("Williams"));
	}

	[Fact]
	public void CultureGroupCorrectlyParsesFamilyNamesWithComplexFormat() {
		var reader = new BufferedReader("""
		{
			family = {
				Stroganov.Stroganova.Stroganovy.Stroganovian
				Romanov.Romanova.Romanovy.Romanovian
				Petrov.Petrova.Petrovy.Petrovian
			}
		}
		""");

		var cultureGroup = new CultureGroup("slavic", reader);

		Assert.Equal("Stroganov", cultureGroup.GetMaleFamilyNameForm("Stroganovy"));
		Assert.Equal("Romanov", cultureGroup.GetMaleFamilyNameForm("Romanovy"));
		Assert.Equal("Petrov", cultureGroup.GetMaleFamilyNameForm("Petrovy"));
	}

	[Fact]
	public void CultureGroupHandlesMixedFamilyNameFormats() {
		var reader = new BufferedReader("""
		{
			family = {
				Smith
				Romanov.Romanova.Romanovy.Romanovian
				Johnson
				Petrov.Petrova.Petrovy.Petrovian
			}
		}
		""");

		var cultureGroup = new CultureGroup("mixed", reader);

		Assert.Equal("Smith", cultureGroup.GetMaleFamilyNameForm("Smith"));
		Assert.Equal("Johnson", cultureGroup.GetMaleFamilyNameForm("Johnson"));
		Assert.Equal("Romanov", cultureGroup.GetMaleFamilyNameForm("Romanovy"));
		Assert.Equal("Petrov", cultureGroup.GetMaleFamilyNameForm("Petrovy"));
	}

	[Fact]
	public void WarningIsLoggedForInvalidFamilyNameFormat() {
		var reader = new BufferedReader("""
		{
			family = {
				Smith
				Romanov.Romanova.Romanovy
				Johnson.Johnsonova
			}
		}
		""");

		var writer = new StringWriter();
		Console.SetOut(writer);
		var cultureGroup = new CultureGroup("test", reader);

		var output = writer.ToString();
		Assert.Contains("[WARN] Unknown family name format: Romanov.Romanova.Romanovy", output);
		Assert.Contains("[WARN] Unknown family name format: Johnson.Johnsonova", output);

		// Valid names should still work
		Assert.Equal("Smith", cultureGroup.GetMaleFamilyNameForm("Smith"));
		// Invalid names should not be found
		Assert.Null(cultureGroup.GetMaleFamilyNameForm("Romanovy"));
		Assert.Null(cultureGroup.GetMaleFamilyNameForm("Johnsonova"));
	}

	[Fact]
	public void GetMaleFamilyNameFormReturnsNullForUnknownKey() {
		var reader = new BufferedReader("""
		{
			family = {
				Smith
				Romanov.Romanova.Romanovy.Romanovian
			}
		}
		""");

		var cultureGroup = new CultureGroup("test", reader);

		Assert.Null(cultureGroup.GetMaleFamilyNameForm("UnknownName"));
		Assert.Null(cultureGroup.GetMaleFamilyNameForm("NonExistent"));
	}

	[Fact]
	public void GetMaleFamilyNameFormDelegatesToCulturesWhenNotFoundInGroup() {
		var reader = new BufferedReader("""
		{
			family = {
				GroupName
			}
			culture = {
				test_culture = {
					family = {
						CultureName
						Petrov.Petrova.Petrovy.Petrovian
					}
				}
			}
		}
		""");

		var cultureGroup = new CultureGroup("test", reader);

		// Should find group-level family name
		Assert.Equal("GroupName", cultureGroup.GetMaleFamilyNameForm("GroupName"));
		
		// Should delegate to culture and find culture-level family name
		Assert.Equal("CultureName", cultureGroup.GetMaleFamilyNameForm("CultureName"));
		Assert.Equal("Petrov", cultureGroup.GetMaleFamilyNameForm("Petrovy"));
		
		// Should return null if not found anywhere
		Assert.Null(cultureGroup.GetMaleFamilyNameForm("NotFound"));
	}

	[Fact]
	public void GetMaleFamilyNameFormSearchesMultipleCultures() {
		var reader = new BufferedReader("""
		{
			culture = {
				culture1 = {
					family = {
						FirstCulture
					}
				}
				culture2 = {
					family = {
						SecondCulture
						Found.Founda.Foundy.Foundian
					}
				}
				culture3 = {
					family = {
						ThirdCulture
					}
				}
			}
		}
		""");

		var cultureGroup = new CultureGroup("multi_culture", reader);

		Assert.Equal("FirstCulture", cultureGroup.GetMaleFamilyNameForm("FirstCulture"));
		Assert.Equal("SecondCulture", cultureGroup.GetMaleFamilyNameForm("SecondCulture"));
		Assert.Equal("Found", cultureGroup.GetMaleFamilyNameForm("Foundy"));
		Assert.Equal("ThirdCulture", cultureGroup.GetMaleFamilyNameForm("ThirdCulture"));
	}

	[Fact]
	public void CultureGroupHandlesEmptyFamilyList() {
		var reader = new BufferedReader("""
		{
			family = {
			}
		}
		""");

		var cultureGroup = new CultureGroup("empty_family", reader);

		Assert.Null(cultureGroup.GetMaleFamilyNameForm("AnyName"));
	}

	[Fact]
	public void CultureGroupIgnoresUnregisteredItems() {
		var reader = new BufferedReader("""
		{
			unknown_keyword = "value"
			another_unknown = {
				nested = "data"
			}
			family = {
				ValidName
			}
		}
		""");

		var cultureGroup = new CultureGroup("ignore_test", reader);

		// Should still work despite unknown keywords
		Assert.Equal("ValidName", cultureGroup.GetMaleFamilyNameForm("ValidName"));
	}

	[Fact]
	public void CultureGroupHandlesComplexNestedStructure() {
		var reader = new BufferedReader("""
		{
			family = {
				GroupFamily1
				Complex.Complexa.Complexy.Complexian
			}
			culture = {
				culture1 = {
					family = {
						Culture1Family
						Simple.Simpla.Simply.Simplian
					}
					unknown_culture_data = "ignored"
				}
				culture2 = {
					family = {
						Culture2Family
					}
				}
			}
			unknown_group_data = "also ignored"
		}
		""");

		var cultureGroup = new CultureGroup("complex", reader);

		Assert.Equal("complex", cultureGroup.Id);
		Assert.Equal(2, cultureGroup.Count);
		
		// Group-level family names
		Assert.Equal("GroupFamily1", cultureGroup.GetMaleFamilyNameForm("GroupFamily1"));
		Assert.Equal("Complex", cultureGroup.GetMaleFamilyNameForm("Complexy"));
		
		// Culture-level family names
		Assert.Equal("Culture1Family", cultureGroup.GetMaleFamilyNameForm("Culture1Family"));
		Assert.Equal("Simple", cultureGroup.GetMaleFamilyNameForm("Simply"));
		Assert.Equal("Culture2Family", cultureGroup.GetMaleFamilyNameForm("Culture2Family"));
		
		// Non-existent names
		Assert.Null(cultureGroup.GetMaleFamilyNameForm("NonExistent"));
	}
}
