using commonItems;
using ImageMagick;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorCharacter = ImperatorToCK3.Imperator.Characters.Character;
using ImperatorToCK3.Imperator.Characters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit.Sdk;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

public sealed class DNAFactoryTests {
	private static void SetNonPublicField(object obj, string fieldName, object value) {
		var field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field is null) {
			throw new XunitException($"Could not find field {fieldName} on {obj.GetType().FullName}.");
		}
		field.SetValue(obj, value);
	}

	private static ImperatorCharacter CreateAdultMale(ulong id = 1, params string[] traits) {
		var character = new ImperatorCharacter(id) {
			Female = false,
			Traits = new List<string>(traits)
		};
		SetNonPublicField(character, "<Age>k__BackingField", (uint)30);
		return character;
	}

	private static PortraitData CreatePortraitDataWithEyeAccessoryGeneTemplate(string geneTemplateName) {
		var portraitData = (PortraitData)FormatterServices.GetUninitializedObject(typeof(PortraitData));
		var dict = new Dictionary<string, AccessoryGeneData> {
			["eye_accessory"] = new AccessoryGeneData {
				GeneTemplate = geneTemplateName,
				ObjectName = "dummy",
				GeneTemplateRecessive = geneTemplateName,
				ObjectNameRecessive = "dummy"
			}
		};
		SetNonPublicField(portraitData, "<AccessoryGenesDict>k__BackingField", dict);
		return portraitData;
	}

	private static GenesDB CreateCk3GenesDbForEyeAccessories() {
		var db = new GenesDB();
		// CK3 accessory gene used for blind eyes (key: eye_accessory, template: blind_eyes)
		db.AccessoryGenes.AddOrReplace(new AccessoryGene(
			"eye_accessory",
			new BufferedReader(
				" = {\n" +
				"  blind_eyes = {\n" +
				"   index = 0\n" +
				"   male = { 1 = blind_obj_a 1 = blind_obj_b }\n" +
				"  }\n" +
				" }\n"
			)
		));

		// CK3 special accessory gene used for eye patch and blindfold (key: special_headgear_spectacles)
		db.SpecialAccessoryGenes.AddOrReplace(new AccessoryGene(
			"special_headgear_spectacles",
			new BufferedReader(
				" = {\n" +
				"  eye_patch = {\n" +
				"   index = 0\n" +
				"   male = { 1 = patch_a 1 = patch_b }\n" +
				"  }\n" +
				"  blindfold = {\n" +
				"   index = 1\n" +
				"   male = { 1 = blindfold_a 1 = blindfold_b }\n" +
				"  }\n" +
				" }\n"
			)
		));

		return db;
	}

	private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
		var method = typeof(DNAFactory).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
		if (method is null) {
			throw new MissingMethodException(typeof(DNAFactory).FullName, methodName);
		}

		return (T)method.Invoke(null, args)!;
	}

	private static void InvokePrivateStaticVoid(string methodName, params object[] args) {
		var method = typeof(DNAFactory).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
		if (method is null) {
			throw new MissingMethodException(typeof(DNAFactory).FullName, methodName);
		}

		method.Invoke(null, args);
	}

	private static MagickImage CreatePaletteImage(int width, int height, IReadOnlyDictionary<(int x, int y), ushort[]> rgbByPixel) {
		var image = new MagickImage(MagickColors.Black, (uint)width, (uint)height);
		var pixels = image.GetPixels();

		foreach (var kvp in rgbByPixel) {
			pixels.SetPixel(kvp.Key.x, kvp.Key.y, kvp.Value);
		}

		return image;
	}

	private static IMagickColor<ushort> ColorAt(IPixelCollection<ushort> pixels, int x, int y) {
		return pixels.GetPixel(x, y).ToColor() ?? throw new InvalidOperationException("Pixel.ToColor() returned null.");
	}

	[Fact]
	public void BuildColorConversionCache_PopulatesCoordinatesForEachPixelColor() {
		// Arrange
		using var image = CreatePaletteImage(
			width: 2,
			height: 2,
			rgbByPixel: new Dictionary<(int x, int y), ushort[]> {
				[(0, 0)] = [65535, 0, 0], // red
				[(1, 0)] = [0, 65535, 0], // green
				[(0, 1)] = [0, 0, 65535], // blue
				[(1, 1)] = [65535, 65535, 0], // yellow
			}
		);
		var pixels = image.GetPixels();

		var dict = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var red = ColorAt(pixels, 0, 0);
		var green = ColorAt(pixels, 1, 0);
		var blue = ColorAt(pixels, 0, 1);
		var yellow = ColorAt(pixels, 1, 1);

		// Act
		InvokePrivateStaticVoid("BuildColorConversionCache", pixels, dict);

		// Assert
		Assert.Equal(4, dict.Count);

		Assert.True(dict.TryGetValue(red, out var redCoords));
		Assert.Equal((ushort)0, redCoords.X);
		Assert.Equal((ushort)0, redCoords.Y);

		Assert.True(dict.TryGetValue(green, out var greenCoords));
		Assert.Equal((ushort)1, greenCoords.X);
		Assert.Equal((ushort)0, greenCoords.Y);

		Assert.True(dict.TryGetValue(blue, out var blueCoords));
		Assert.Equal((ushort)0, blueCoords.X);
		Assert.Equal((ushort)1, blueCoords.Y);

		Assert.True(dict.TryGetValue(yellow, out var yellowCoords));
		Assert.Equal((ushort)1, yellowCoords.X);
		Assert.Equal((ushort)1, yellowCoords.Y);
	}

	[Fact]
	public void GetCoordinatesOfClosestCK3Color_WhenExactMatch_ReturnsExistingCoordinatesWithoutAddingKey() {
		// Arrange
		var dict = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();
		var color = new MagickColor("#ff0000");
		var expected = new DNA.PaletteCoordinates { X = 10, Y = 20 };
		dict[color] = expected;
		var beforeCount = dict.Count;

		// Act
		var actual = InvokePrivateStatic<DNA.PaletteCoordinates>("GetCoordinatesOfClosestCK3Color", color, dict);

		// Assert
		Assert.Equal(expected.X, actual.X);
		Assert.Equal(expected.Y, actual.Y);
		Assert.Equal(beforeCount, dict.Count);
	}

	[Fact]
	public void GetCoordinatesOfClosestCK3Color_WhenMissing_AddsQueryColorToCacheMappedToClosest() {
		// Arrange
		var dict = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var black = new MagickColor("#000000");
		var white = new MagickColor("#ffffff");

		var blackCoords = new DNA.PaletteCoordinates { X = 1, Y = 1 };
		var whiteCoords = new DNA.PaletteCoordinates { X = 2, Y = 2 };

		dict[black] = blackCoords;
		dict[white] = whiteCoords;

		// Near-white (not exactly white)
		var nearWhite = new MagickColor("#fffffe");

		// Act
		var actual = InvokePrivateStatic<DNA.PaletteCoordinates>("GetCoordinatesOfClosestCK3Color", nearWhite, dict);

		// Assert: should map to white
		Assert.Equal(whiteCoords.X, actual.X);
		Assert.Equal(whiteCoords.Y, actual.Y);

		Assert.True(dict.TryGetValue(nearWhite, out var cached));
		Assert.Equal(whiteCoords.X, cached.X);
		Assert.Equal(whiteCoords.Y, cached.Y);
	}

	[Fact]
	public void GetCoordinatesOfClosestCK3Color_UsesEuclideanDistance_SelectsNearest() {
		// Arrange
		var dict = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var red = new MagickColor("#ff0000");
		var green = new MagickColor("#00ff00");

		var redCoords = new DNA.PaletteCoordinates { X = 3, Y = 4 };
		var greenCoords = new DNA.PaletteCoordinates { X = 9, Y = 9 };

		dict[red] = redCoords;
		dict[green] = greenCoords;

		// Closer to red than green
		var nearRed = new MagickColor("#f10000");

		// Act
		var actual = InvokePrivateStatic<DNA.PaletteCoordinates>("GetCoordinatesOfClosestCK3Color", nearRed, dict);

		// Assert
		Assert.Equal(redCoords.X, actual.X);
		Assert.Equal(redCoords.Y, actual.Y);
	}

	[Fact]
	public void GetPaletteCoordinates_MapsIRPalettePixelColorToClosestCK3Coordinates() {
		// Arrange
		using var irImage = CreatePaletteImage(
			width: 4,
			height: 4,
			rgbByPixel: new Dictionary<(int x, int y), ushort[]> {
				[(2, 3)] = new ushort[] { 1000, 2000, 3000 },
			}
		);
		var irPixels = irImage.GetPixels();
		var irColor = ColorAt(irPixels, 2, 3);

		var ck3Dict = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();
		var expected = new DNA.PaletteCoordinates { X = 123, Y = 456 };
		ck3Dict[irColor] = expected;

		var irCoords = new PaletteCoordinates { X = 2, Y = 3 };

		// Act
		var actual = InvokePrivateStatic<DNA.PaletteCoordinates>("GetPaletteCoordinates", irCoords, irPixels, ck3Dict);

		// Assert
		Assert.Equal(expected.X, actual.X);
		Assert.Equal(expected.Y, actual.Y);
	}

	[Theory]
	[InlineData("eyepatch_1")]
	[InlineData("eyepatch_2")]
	public void ConvertEyeAccessories_EyePatchTemplates_AddSpecialHeadgearSpectacles(string irGeneTemplateName) {
		var character = CreateAdultMale();
		var portraitData = CreatePortraitDataWithEyeAccessoryGeneTemplate(irGeneTemplateName);
		var ck3GenesDb = CreateCk3GenesDbForEyeAccessories();
		var eyeColorCache = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var colorDna = new Dictionary<string, DNAColorGeneValue> {
			["eye_color"] = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 }
		};
		var accessoryDna = new Dictionary<string, DNAAccessoryGeneValue>();

		InvokePrivateStaticVoid(
			"ConvertEyeAccessories",
			character,
			portraitData,
			colorDna,
			accessoryDna,
			ck3GenesDb,
			eyeColorCache
		);

		Assert.True(accessoryDna.TryGetValue("special_headgear_spectacles", out var v));
		Assert.Equal("eye_patch", v.TemplateName);
		Assert.Equal("patch_b", v.ObjectName);
	}

	[Fact]
	public void ConvertEyeAccessories_BlindfoldTemplate_AddsSpecialHeadgearSpectaclesBlindfold() {
		var character = CreateAdultMale();
		var portraitData = CreatePortraitDataWithEyeAccessoryGeneTemplate("blindfold_1");
		var ck3GenesDb = CreateCk3GenesDbForEyeAccessories();
		var eyeColorCache = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var colorDna = new Dictionary<string, DNAColorGeneValue> {
			["eye_color"] = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 }
		};
		var accessoryDna = new Dictionary<string, DNAAccessoryGeneValue>();

		InvokePrivateStaticVoid(
			"ConvertEyeAccessories",
			character,
			portraitData,
			colorDna,
			accessoryDna,
			ck3GenesDb,
			eyeColorCache
		);

		Assert.True(accessoryDna.TryGetValue("special_headgear_spectacles", out var v));
		Assert.Equal("blindfold", v.TemplateName);
		Assert.Equal("blindfold_b", v.ObjectName);
	}

	[Fact]
	public void ConvertEyeAccessories_BlindEyesTemplate_AddsEyeAccessoryBlindEyes() {
		var character = CreateAdultMale();
		var portraitData = CreatePortraitDataWithEyeAccessoryGeneTemplate("blind_eyes");
		var ck3GenesDb = CreateCk3GenesDbForEyeAccessories();
		var eyeColorCache = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var colorDna = new Dictionary<string, DNAColorGeneValue> {
			["eye_color"] = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 }
		};
		var accessoryDna = new Dictionary<string, DNAAccessoryGeneValue>();

		InvokePrivateStaticVoid(
			"ConvertEyeAccessories",
			character,
			portraitData,
			colorDna,
			accessoryDna,
			ck3GenesDb,
			eyeColorCache
		);

		Assert.True(accessoryDna.TryGetValue("eye_accessory", out var v));
		Assert.Equal("blind_eyes", v.TemplateName);
		Assert.Equal("blind_obj_b", v.ObjectName);
	}

	[Fact]
	public void ConvertEyeAccessories_RedEyesTemplate_OverridesEyeColorToRed() {
		var character = CreateAdultMale();
		var portraitData = CreatePortraitDataWithEyeAccessoryGeneTemplate("red_eyes");
		var ck3GenesDb = CreateCk3GenesDbForEyeAccessories();
		var eyeColorCache = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();
		eyeColorCache[new MagickColor("#ff0000")] = new DNA.PaletteCoordinates { X = 100, Y = 200 };

		var original = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 };
		var colorDna = new Dictionary<string, DNAColorGeneValue> {
			["eye_color"] = original
		};
		var accessoryDna = new Dictionary<string, DNAAccessoryGeneValue>();

		InvokePrivateStaticVoid(
			"ConvertEyeAccessories",
			character,
			portraitData,
			colorDna,
			accessoryDna,
			ck3GenesDb,
			eyeColorCache
		);

		Assert.True(colorDna.TryGetValue("eye_color", out var updated));
		Assert.Equal((byte)50, updated.X);
		Assert.Equal((byte)100, updated.Y);
		Assert.Equal(original.XRecessive, updated.XRecessive);
		Assert.Equal(original.YRecessive, updated.YRecessive);
	}

	[Fact]
	public void ConvertEyeAccessories_BlindTrait_AddsBlindEyesAndBlindfold() {
		var character = CreateAdultMale(1, "blind");
		var portraitData = CreatePortraitDataWithEyeAccessoryGeneTemplate("normal_eyes");
		var ck3GenesDb = CreateCk3GenesDbForEyeAccessories();
		var eyeColorCache = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var colorDna = new Dictionary<string, DNAColorGeneValue> {
			["eye_color"] = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 }
		};
		var accessoryDna = new Dictionary<string, DNAAccessoryGeneValue>();

		InvokePrivateStaticVoid(
			"ConvertEyeAccessories",
			character,
			portraitData,
			colorDna,
			accessoryDna,
			ck3GenesDb,
			eyeColorCache
		);

		Assert.True(accessoryDna.TryGetValue("eye_accessory", out var eyes));
		Assert.Equal("blind_eyes", eyes.TemplateName);

		Assert.True(accessoryDna.TryGetValue("special_headgear_spectacles", out var blindfold));
		Assert.Equal("blindfold", blindfold.TemplateName);
	}

	[Fact]
	public void ConvertEyeAccessories_OneEyedTrait_AddsEyePatch() {
		var character = CreateAdultMale(1, "one_eyed");
		var portraitData = CreatePortraitDataWithEyeAccessoryGeneTemplate("normal_eyes");
		var ck3GenesDb = CreateCk3GenesDbForEyeAccessories();
		var eyeColorCache = new ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates>();

		var colorDna = new Dictionary<string, DNAColorGeneValue> {
			["eye_color"] = new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 }
		};
		var accessoryDna = new Dictionary<string, DNAAccessoryGeneValue>();

		InvokePrivateStaticVoid(
			"ConvertEyeAccessories",
			character,
			portraitData,
			colorDna,
			accessoryDna,
			ck3GenesDb,
			eyeColorCache
		);

		Assert.True(accessoryDna.TryGetValue("special_headgear_spectacles", out var v));
		Assert.Equal("eye_patch", v.TemplateName);
		Assert.Equal("patch_b", v.ObjectName);
	}
}
