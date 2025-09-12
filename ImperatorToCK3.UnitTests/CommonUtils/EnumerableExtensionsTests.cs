using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;
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
        IEnumerable<int> numbers = new int[0];

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
}
