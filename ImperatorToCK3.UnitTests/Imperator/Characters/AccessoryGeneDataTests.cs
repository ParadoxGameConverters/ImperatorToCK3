using ImperatorToCK3.Imperator.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters;
public class AccessoryGeneDataTests {
    [Fact]
    public void Equals_ReturnsTrue_ForIdenticalData() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        var data2 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        Assert.True(data1.Equals(data2));
        Assert.True(data1 == data2);
        Assert.False(data1 != data2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentGeneTemplate() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        var data2 = new AccessoryGeneData {
            GeneTemplate = "templateX",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        Assert.False(data1.Equals(data2));
        Assert.False(data1 == data2);
        Assert.True(data1 != data2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentObjectName() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        var data2 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "objectX",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        Assert.False(data1.Equals(data2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentGeneTemplateRecessive() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        var data2 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "templateX",
            ObjectNameRecessive = "object2"
        };
        Assert.False(data1.Equals(data2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentObjectNameRecessive() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        var data2 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "objectX"
        };
        Assert.False(data1.Equals(data2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForNullObject() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        object? obj = null;
        Assert.False(data1.Equals(obj));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentObjectType() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        object obj = "not a AccessoryGeneData";
        Assert.False(data1.Equals(obj));
    }

    [Fact]
    public void GetHashCode_IsConsistentWithEquals() {
        var data1 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        var data2 = new AccessoryGeneData {
            GeneTemplate = "template1",
            ObjectName = "object1",
            GeneTemplateRecessive = "template2",
            ObjectNameRecessive = "object2"
        };
        Assert.Equal(data1.GetHashCode(), data2.GetHashCode());
    }
}
