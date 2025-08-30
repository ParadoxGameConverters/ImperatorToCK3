using ImperatorToCK3.Imperator.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters;

public class MorphGeneDataTests {
    private static MorphGeneData Create(
        string tpl = "template1",
        byte val = 10,
        string tplRec = "template2",
        byte valRec = 20) => new() {
            TemplateName = tpl,
            Value = val,
            TemplateRecessiveName = tplRec,
            ValueRecessive = valRec
        };

    [Fact]
    public void Equals_ReturnsTrue_ForIdenticalData() {
        var d1 = Create();
        var d2 = Create();
        Assert.True(d1.Equals(d2));
        Assert.True(d1 == d2);
        Assert.False(d1 != d2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentTemplateName() {
        var d1 = Create();
        var d2 = Create(tpl: "other");
        Assert.False(d1.Equals(d2));
        Assert.False(d1 == d2);
        Assert.True(d1 != d2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentValue() {
        var d1 = Create();
        var d2 = Create(val: 11);
        Assert.False(d1.Equals(d2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentTemplateRecessiveName() {
        var d1 = Create();
        var d2 = Create(tplRec: "otherRec");
        Assert.False(d1.Equals(d2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentValueRecessive() {
        var d1 = Create();
        var d2 = Create(valRec: 21);
        Assert.False(d1.Equals(d2));
    }

    [Fact]
    public void EqualsObject_ReturnsFalse_ForNull() {
        var d1 = Create();
        object? obj = null;
        Assert.False(d1.Equals(obj));
    }

    [Fact]
    public void EqualsObject_ReturnsFalse_ForDifferentType() {
        var d1 = Create();
        object obj = "not a MorphGeneData";
        Assert.False(d1.Equals(obj));
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues() {
        var d1 = Create();
        var d2 = Create();
        Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DiffersWhenAFieldDiffers() {
        var d1 = Create();
        var d2 = Create(val: 11); // change one field
        Assert.NotEqual(d1.GetHashCode(), d2.GetHashCode());
    }

    [Fact]
    public void OperatorEqualityMatchesEquals() {
        var d1 = Create();
        var d2 = Create();
        Assert.Equal(d1.Equals(d2), d1 == d2);
    }

    [Fact]
    public void OperatorInequalityMatchesNegatedEquals() {
        var d1 = Create();
        var d2 = Create(valRec: 30);
        Assert.Equal(!d1.Equals(d2), d1 != d2);
    }
}
