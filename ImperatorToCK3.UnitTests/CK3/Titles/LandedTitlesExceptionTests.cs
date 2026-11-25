using AwesomeAssertions;
using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Reflection;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles;

[Collection("Sequential")]
public class LandedTitlesExceptionTests {
	private static readonly ColorFactory colorFactory = new();

	[Fact]
	public void CleanUpHistory_ShouldNotThrow_WhenLiegeHasNoHolderField() {
		var titles = new Title.LandedTitles();
		// Create liege title with no holder history at all.
		var liegeTitle = titles.Add("k_liege");
		// Ensure it has no holder field.

		// Create vassal title.
		var vassalTitle = titles.Add("d_vassal");
		// Add barony and county to make vassal valid for liege.
		var barony = titles.Add("b_barony");
		barony.History.AddFieldValue(new Date(1, 1, 1), "province", "province", (ulong)1);
		var county = titles.Add("c_county");
		county.History.AddFieldValue(new Date(1, 1, 1), "province", "province", (ulong)1);
		// Link de jure for completeness (not required for the bug, but helps).
		barony.GetType().GetProperty("DeJureLiege")?.SetValue(barony, county);
		county.GetType().GetProperty("DeJureLiege")?.SetValue(county, vassalTitle);
		// Alternative: use internal API if available.

		// Set vassal's liege to k_liege at a date where liege has no holder.
		var liegeDate = new Date(860, 1, 1);
		var bookmarkDate = new Date(867, 1, 1);
		vassalTitle.History.AddFieldValue(liegeDate, "liege", "liege", "k_liege");

		var characters = new CharacterCollection();
		var holder = new Character("1", "Holder", new Date(800, 1, 1), characters);
		characters.Add(holder);
		// Give the vassal a holder so it is not ignored.
		vassalTitle.SetHolder(holder, liegeDate);

		// Act: should not throw.
		Action act = () => titles.CleanUpHistory(characters, bookmarkDate);
		act.Should().NotThrow("CleanUpHistory should handle liege with missing holder field safely");

		// After cleanup, the invalid liege entry should be removed because liege has no holder.
		vassalTitle.GetLiegeId(liegeDate).Should().BeNullOrEmpty("invalid liege should be removed");
	}

	[Fact]
	public void CleanUpHistory_ShouldNotThrow_WhenLiegeHolderHistoryIsEmpty() {
		// Liege has holder field but no dated entries that satisfy the filter.
		var titles = new Title.LandedTitles();
		var liegeTitle = titles.Add("k_liege2");
		// Add a holder entry that is before the liege date and is 0, so filtered set is empty.
		// We need a holder field with an entry that is not > liegeDate.
		liegeTitle.History.AddFieldValue(new Date(850, 1, 1), "holder", "holder", "0");

		var vassalTitle = titles.Add("d_vassal2");
		var liegeDate = new Date(860, 1, 1);
		var bookmarkDate = new Date(867, 1, 1);
		vassalTitle.History.AddFieldValue(liegeDate, "liege", "liege", "k_liege2");

		var characters = new CharacterCollection();
		var holder = new Character("2", "Holder2", new Date(800, 1, 1), characters);
		characters.Add(holder);
		vassalTitle.SetHolder(holder, liegeDate);

		Action act = () => titles.CleanUpHistory(characters, bookmarkDate);
		act.Should().NotThrow("empty filtered holder set should be handled via FirstOrDefault, not Min");
	}

	[Fact]
	public void CleanUpTitlesHavingInvalidCapitalCounties_ShouldNotThrow_WhenNoCountyExists() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader("e_empire = { capital = c_nonexistent }"), colorFactory);
		titles.LoadTitles(new BufferedReader("k_kingdom = { capital = c_nonexistent }"), colorFactory);
		// At this point, validTitleIds contains only e_empire, k_kingdom, etc., no c_.

		// The private method CleanUpTitlesHavingInvalidCapitalCounties is called inside LoadTitles,
		// but we also invoke it via reflection to ensure it handles empty county case.
		var method = typeof(Title.LandedTitles).GetMethod("CleanUpTitlesHavingInvalidCapitalCounties", BindingFlags.NonPublic | BindingFlags.Instance);
		method.Should().NotBeNull();

		Action act = () => method!.Invoke(titles, null);
		act.Should().NotThrow("placeholder lookup should use FirstOrDefault and return early when no county exists");
	}
}
