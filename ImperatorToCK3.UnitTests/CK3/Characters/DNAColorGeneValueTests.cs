using ImperatorToCK3.CK3.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

public class DNAColorGeneValueTests {
	[Fact]
	public void DNAColorGeneValueIsInitialized() {
		var colorGeneValue = new DNAColorGeneValue {
			X = 1,
			Y = 2,
			XRecessive = 3,
			YRecessive = 4
		};
		Assert.Equal(1, colorGeneValue.X);
		Assert.Equal(2, colorGeneValue.Y);
		Assert.Equal(3, colorGeneValue.XRecessive);
		Assert.Equal(4, colorGeneValue.YRecessive);
	}

	[Fact]
	public void DNAColorGeneValueIsCorrectlyConvertedToString() {
		var colorGeneValue = new DNAColorGeneValue {
			X = 1,
			Y = 2,
			XRecessive = 3,
			YRecessive = 4
		};
		Assert.Equal("1 2 3 4", colorGeneValue.ToString());
	}

	[Fact]
	public void DNAColorGeneValueEqualsReturnsTrueForEqualValues() {
		var a = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };
		var b = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };

		Assert.True(a.Equals(b));
		Assert.True(a.Equals((object)b));
	}

	[Fact]
	public void DNAColorGeneValueEqualsReturnsFalseForDifferentValues() {
		var a = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };
		var b = new DNAColorGeneValue { X = 9, Y = 2, XRecessive = 3, YRecessive = 4 };

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void DNAColorGeneValueEqualsReturnsFalseForNonGeneValue() {
		var a = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };

		Assert.False(a.Equals(null));
		Assert.False(a.Equals("not a gene value"));
		Assert.False(a.Equals(new object()));
	}

	[Fact]
	public void DNAColorGeneValueGetHashCodeIsEqualForEqualValues() {
		var a = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };
		var b = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };

		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void DNAColorGeneValueEqualityOperatorReturnsTrueForEqualValues() {
		var a = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };
		var b = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };

		Assert.True(a == b);
		Assert.False(a != b);
	}

	[Fact]
	public void DNAColorGeneValueInequalityOperatorReturnsTrueForDifferentValues() {
		var a = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };
		var b = new DNAColorGeneValue { X = 9, Y = 2, XRecessive = 3, YRecessive = 4 };

		Assert.True(a != b);
		Assert.False(a == b);
	}
}
