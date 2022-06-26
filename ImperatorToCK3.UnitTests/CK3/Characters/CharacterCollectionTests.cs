using commonItems;
using commonItems.Localization;
using FluentAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CharacterCollectionTests {
	[Fact]
	public void MarriageDateCanBeEstimatedFromChild() {
		var endDate = new Date(1100, 1, 1, AUC: true);
		var configuration = new Configuration { CK3BookmarkDate = endDate };
		var imperatorWorld = new World(configuration);

		var male = new ImperatorToCK3.Imperator.Characters.Character(1);
		var childReader = new BufferedReader("father=1 mother=2 birth_date=900.1.1");
		var child = ImperatorToCK3.Imperator.Characters.Character.Parse(childReader, "3", null);
		var female = new ImperatorToCK3.Imperator.Characters.Character(2);

		male.Spouses.Add(1, female);
		male.Children.Add(3, child);
		female.Children.Add(3, child);
		imperatorWorld.Characters.Add(male);
		imperatorWorld.Characters.Add(female);
		imperatorWorld.Characters.Add(child);
		var imperatorRegionMapper = new ImperatorRegionMapper();
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3Characters = new CharacterCollection();
		ck3Characters.ImportImperatorCharacters(
			imperatorWorld,
			new ReligionMapper(imperatorRegionMapper, ck3RegionMapper),
			new CultureMapper(imperatorRegionMapper, ck3RegionMapper),
			new TraitMapper(),
			new NicknameMapper(),
			new LocDB("english"),
			new ProvinceMapper(),
			new DeathReasonMapper(),
			endDate,
			configuration);

		Assert.Collection(ck3Characters,
			ck3Male => {
				var marriageDate = ck3Male.History.Fields["spouses"].DateToEntriesDict.FirstOrDefault().Key;
				Assert.Equal(new Date(899, 3, 27, AUC: true), marriageDate);
			},
			ck3Female => {
				Assert.Equal("imperator2", ck3Female.Id);
			},
			ck3Child => {
				Assert.Equal("imperator3", ck3Child.Id);
			});
	}

	[Fact]
	public void MarriageDateCanBeEstimatedFromUnbornChild() {
		var endDate = new Date(1100, 1, 1, AUC: true);
		var configuration = new Configuration { CK3BookmarkDate = endDate };
		var imperatorWorld = new World(configuration);

		var male = new ImperatorToCK3.Imperator.Characters.Character(1);
		var femaleReader = new BufferedReader("unborn={ { mother=2 father=1 date=900.1.1 } }");
		var female = ImperatorToCK3.Imperator.Characters.Character.Parse(femaleReader, "2", null);

		male.Spouses.Add(1, female);
		imperatorWorld.Characters.Add(male);
		imperatorWorld.Characters.Add(female);

		var imperatorRegionMapper = new ImperatorRegionMapper();
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3Characters = new CharacterCollection();
		ck3Characters.ImportImperatorCharacters(
			imperatorWorld,
			new ReligionMapper(imperatorRegionMapper, ck3RegionMapper),
			new CultureMapper(imperatorRegionMapper, ck3RegionMapper),
			new TraitMapper(),
			new NicknameMapper(),
			new LocDB("english"),
			new ProvinceMapper(),
			new DeathReasonMapper(),
			endDate,
			configuration);

		Assert.Collection(ck3Characters,
			ck3Male => {
				Assert.Equal(new Date(899, 3, 27, AUC: true),
					ck3Male.History.Fields["spouses"].DateToEntriesDict.FirstOrDefault().Key);
			},
			ck3Female => Assert.Equal("imperator2", ck3Female.Id)
		);
	}

	[Fact]
	public void OnlyEarlyPregnanciesAreImportedFromImperator() {
		var conversionDate = new Date(900, 2, 1, AUC: true);
		var configuration = new Configuration { CK3BookmarkDate = conversionDate };
		var imperatorWorld = new World(configuration);

		var male = new ImperatorToCK3.Imperator.Characters.Character(1);

		var female1Reader = new BufferedReader("female=yes unborn={ { mother=2 father=1 date=900.9.1 } }");
		// child will be born 7 months after conversion date, will be imported
		var female1 = ImperatorToCK3.Imperator.Characters.Character.Parse(female1Reader, "2", null);

		var female2Reader = new BufferedReader("female=yes unborn={ { mother=3 father=1 date=900.10.1 is_bastard=yes } }");
		// child will be born 8 months after conversion date, will be imported
		var female2 = ImperatorToCK3.Imperator.Characters.Character.Parse(female2Reader, "3", null);

		var female3Reader = new BufferedReader("female=yes unborn={ { mother=3 father=1 date=900.6.1 } }");
		// child will be born 4 months after conversion date, will not be imported
		var female3 = ImperatorToCK3.Imperator.Characters.Character.Parse(female3Reader, "4", null);

		imperatorWorld.Characters.Add(male);
		imperatorWorld.Characters.Add(female1);
		imperatorWorld.Characters.Add(female2);
		imperatorWorld.Characters.Add(female3);

		var imperatorRegionMapper = new ImperatorRegionMapper();
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3Characters = new CharacterCollection();
		ck3Characters.ImportImperatorCharacters(
			imperatorWorld,
			new ReligionMapper(imperatorRegionMapper, ck3RegionMapper),
			new CultureMapper(imperatorRegionMapper, ck3RegionMapper),
			new TraitMapper(),
			new NicknameMapper(),
			new LocDB("english"),
			new ProvinceMapper(),
			new DeathReasonMapper(),
			conversionDate,
			configuration);

		ck3Characters["imperator2"].Pregnancies
			.Should()
			.ContainEquivalentOf(new Pregnancy("imperator1", "imperator2", new Date(900, 9, 1, AUC: true), isBastard: false));
		ck3Characters["imperator3"].Pregnancies
			.Should()
			.ContainEquivalentOf(new Pregnancy("imperator1", "imperator3", new Date(900, 10, 1, AUC: true), isBastard: true));
		ck3Characters["imperator4"].Pregnancies.Should().BeEmpty();
	}
}