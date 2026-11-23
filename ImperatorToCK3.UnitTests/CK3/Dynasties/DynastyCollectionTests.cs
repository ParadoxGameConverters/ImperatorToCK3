using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Countries;
using System;
using System.Reflection;
using Xunit;

using CK3Character = ImperatorToCK3.CK3.Characters.Character;
using ImperatorCharacter = ImperatorToCK3.Imperator.Characters.Character;

namespace ImperatorToCK3.UnitTests.CK3.Dynasties;

public class DynastyCollectionTests {
	private static readonly Date BookmarkDate = new(867, 1, 1);

	private static DynastyCollection MakeCollectionWithDynasty(string dynastyId, StringOfItem? initialCoA = null) {
		var dynasties = new DynastyCollection();
		var dynasty = new Dynasty(dynastyId, new BufferedReader(""));
		if (initialCoA is not null) {
			dynasty.CoA = initialCoA;
		}
		dynasties.AddOrReplace(dynasty);
		return dynasties;
	}

	private static Country MakeCountryWithMonarch(string? dynastyId) {
		var ck3Characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();
		var ck3Monarch = new CK3Character("ck3monarch", "Monarch", new Date(800, 1, 1), ck3Characters);
		if (dynastyId is not null) {
			ck3Monarch.SetDynastyId(dynastyId, null);
		}

		var irMonarch = new ImperatorCharacter(0);
		irMonarch.CK3Character = ck3Monarch;

		var country = new Country(1);
		SetPrivateProperty(country, "Monarch", irMonarch);
		return country;
	}

	private static Title AddTitle(Title.LandedTitles titles, string id, string? coa, Country? country) {
		var title = titles.Add(id);
		if (coa is not null) {
			SetPrivateProperty(title, "CoA", coa);
		}
		if (country is not null) {
			SetPrivateProperty(title, "ImperatorCountry", country);
		}
		return title;
	}

	private static void SetPrivateProperty(object target, string propertyName, object? value) {
		var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException($"Property {propertyName} not found on {target.GetType()}.");
		prop.SetValue(target, value);
	}

	[Fact]
	public void SetCoasForRulingDynasties_SetsCoAFromTitleForRulingDynasty() {
		var dynasties = MakeCollectionWithDynasty("dynn_test");
		var titles = new Title.LandedTitles();
		AddTitle(titles, "k_test", "coa_xyz", MakeCountryWithMonarch("dynn_test"));

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_test", out var dynasty));
		Assert.NotNull(dynasty!.CoA);
		Assert.Equal("k_test", dynasty.CoA!.ToString());
	}

	[Fact]
	public void SetCoasForRulingDynasties_DoesNotOverwriteExistingDynastyCoA() {
		var dynasties = MakeCollectionWithDynasty("dynn_test", new StringOfItem("existing_coa"));
		var titles = new Title.LandedTitles();
		AddTitle(titles, "k_test", "coa_xyz", MakeCountryWithMonarch("dynn_test"));

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_test", out var dynasty));
		Assert.NotNull(dynasty!.CoA);
		Assert.Equal("existing_coa", dynasty.CoA!.ToString());
	}

	[Fact]
	public void SetCoasForRulingDynasties_SkipsTitleWithoutCoA() {
		var dynasties = MakeCollectionWithDynasty("dynn_test");
		var titles = new Title.LandedTitles();
		AddTitle(titles, "k_test", coa: null, MakeCountryWithMonarch("dynn_test"));

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_test", out var dynasty));
		Assert.Null(dynasty!.CoA);
	}

	[Fact]
	public void SetCoasForRulingDynasties_SkipsTitleWithoutImperatorCountry() {
		var dynasties = MakeCollectionWithDynasty("dynn_test");
		var titles = new Title.LandedTitles();
		AddTitle(titles, "k_test", "coa_xyz", country: null);

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_test", out var dynasty));
		Assert.Null(dynasty!.CoA);
	}

	[Fact]
	public void SetCoasForRulingDynasties_SkipsTitleWhenMonarchHasNoDynastyId() {
		var dynasties = MakeCollectionWithDynasty("dynn_test");
		var titles = new Title.LandedTitles();
		AddTitle(titles, "k_test", "coa_xyz", MakeCountryWithMonarch(dynastyId: null));

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_test", out var dynasty));
		Assert.Null(dynasty!.CoA);
	}

	[Fact]
	public void SetCoasForRulingDynasties_SkipsTitleWhenDynastyIsNotInCollection() {
		var dynasties = MakeCollectionWithDynasty("dynn_test");
		var titles = new Title.LandedTitles();
		// Monarch's dynasty id is not present in the collection.
		AddTitle(titles, "k_test", "coa_xyz", MakeCountryWithMonarch("dynn_other"));

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_test", out var dynasty));
		Assert.Null(dynasty!.CoA);
	}

	[Fact]
	public void SetCoasForRulingDynasties_AppliesCoAToEachRulingDynasty() {
		var dynasties = MakeCollectionWithDynasty("dynn_a");
		dynasties.AddOrReplace(new Dynasty("dynn_b", new BufferedReader("")));

		var titles = new Title.LandedTitles();
		AddTitle(titles, "k_a", "coa_a", MakeCountryWithMonarch("dynn_a"));
		AddTitle(titles, "k_b", "coa_b", MakeCountryWithMonarch("dynn_b"));

		dynasties.SetCoasForRulingDynasties(titles, BookmarkDate);

		Assert.True(dynasties.TryGetValue("dynn_a", out var dynastyA));
		Assert.True(dynasties.TryGetValue("dynn_b", out var dynastyB));
		Assert.Equal("k_a", dynastyA!.CoA!.ToString());
		Assert.Equal("k_b", dynastyB!.CoA!.ToString());
	}
}
