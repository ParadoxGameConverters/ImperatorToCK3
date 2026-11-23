using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles;

public class TitleHistoryTests {
	private const string CK3Root = "TestFiles/TitleHistoryTests/CK3/game";
	private readonly ModFilesystem ck3ModFS = new(CK3Root, new List<Mod>());
	private static readonly Date BookmarkDate = new(867, 1, 1);
	[Fact]
	public void HolderDefaultsToZeroString() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Equal("0", title.GetHolderId(new Date(867, 1, 1)));
	}

	[Fact]
	public void LiegeIdDefaultsToNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Null(title.GetLiegeId(new Date(867, 1, 1)));
	}

	[Fact]
	public void GovernmentDefaultsToNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Null(title.GetGovernment(new Date(867, 1, 1)));
	}

	[Fact]
	public void DevelopmentLevelDefaultsToNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Null(title.GetDevelopmentLevel(new Date(867, 1, 1)));
	}

	[Fact]
	public void QuotedGovernmentIsLoadedFromHistory() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("c_quoted_gov");
		titles.LoadHistory(new Configuration { CK3BookmarkDate = BookmarkDate }, ck3ModFS);

		Assert.Equal("quoted_government", title.GetGovernment(BookmarkDate));
	}

	[Fact]
	public void NumericDevelopmentLevelIsLoadedFromHistory() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("c_string_dev");
		titles.LoadHistory(new Configuration { CK3BookmarkDate = BookmarkDate }, ck3ModFS);

		Assert.Equal(15, title.GetDevelopmentLevel(BookmarkDate));
	}

	[Fact]
	public void NonNumericDevelopmentLevelIsLoadedAsNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("c_invalid_dev");
		titles.LoadHistory(new Configuration { CK3BookmarkDate = BookmarkDate }, ck3ModFS);

		Assert.Null(title.GetDevelopmentLevel(new Date(800, 1, 1)));
	}

	[Fact]
	public void StringOfItemGovernmentIsReturned() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");
		var date = new Date(867, 1, 1);
		title.History.AddFieldValue(date, "government", "government", new StringOfItem("quoted_government"));

		Assert.Equal("quoted_government", title.GetGovernment(date));
	}

	[Fact]
	public void StringDevelopmentLevelIsParsed() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("c_county");
		var date = new Date(867, 1, 1);
		title.History.AddFieldValue(date, "development_level", "change_development_level", "15");

		Assert.Equal(15, title.GetDevelopmentLevel(date));
	}
}