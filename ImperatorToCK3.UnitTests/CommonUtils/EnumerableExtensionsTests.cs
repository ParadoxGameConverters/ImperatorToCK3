using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class EnumerableExtensionsTests {
    [Fact]
    public void LastOrNull_ReturnsLastMatching_WhenMatchesExist() {
        // Arrange
        IEnumerable<int> numbers = new[] { 1, 2, 3, 4, 5 };

        // Act
        int? result = numbers.LastOrNull(n => n % 2 == 0);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void LastOrNull_ReturnsNull_WhenNoMatch() {
        // Arrange
        IEnumerable<int> numbers = new[] { 1, 3, 5, 7 };

        // Act
        int? result = numbers.LastOrNull(n => n % 2 == 0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LastOrNull_ReturnsNull_WhenSourceEmpty() {
        // Arrange
        IEnumerable<int> numbers = System.Array.Empty<int>();

        // Act
        int? result = numbers.LastOrNull(n => n > 0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LastOrNull_WorksWithNonMaterializedEnumerable() {
        // Arrange: use a generator to ensure the extension materializes correctly
        IEnumerable<int> numbers = Generate(1, 6); // yields 1..5

        // Act
        int? result = numbers.LastOrNull(n => n > 2);

        // Assert
        Assert.Equal(5, result);

        static IEnumerable<int> Generate(int startInclusive, int endExclusive) {
            for (int i = startInclusive; i < endExclusive; ++i) {
                yield return i;
            }
        }
    }

    [Fact]
    public void LastOrNull_KeyValuePairs_ReturnsLastPair_WhenSourceNotEmpty() {
        // Arrange
        IEnumerable<KeyValuePair<string, int>> pairs = new[] {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("c", 3)
        };

        // Act
        KeyValuePair<string, int>? result = pairs.LastOrNull();

        // Assert
        Assert.Equal(new KeyValuePair<string, int>("c", 3), result);
    }

    [Fact]
    public void LastOrNull_KeyValuePairs_ReturnsNull_WhenSourceEmpty() {
        // Arrange
        IEnumerable<KeyValuePair<string, int>> pairs = System.Array.Empty<KeyValuePair<string, int>>();

        // Act
        KeyValuePair<string, int>? result = pairs.LastOrNull();

        // Assert
        Assert.Null(result);
    }
}
