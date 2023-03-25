using commonItems;
using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters; 

public sealed class DNAFactory {
	private readonly IUnsafePixelCollection<ushort> irHairPalettePixels;
	private readonly IUnsafePixelCollection<ushort> irSkinPalettePixels;
	private readonly IUnsafePixelCollection<ushort> irEyePalettePixels;
	
	private readonly Dictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3HairColorToPaletteCoordinatesDict = new();
	private readonly Dictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3SkinColorToPaletteCoordinatesDict = new();
	private readonly Dictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3EyeColorToPaletteCoordinatesDict = new();
	
	private readonly GenesDB ck3GenesDB;
	private readonly AccessoryGeneMapper accessoryGeneMapper = new("configurables/accessory_genes_map.txt");
	private readonly MorphGeneTemplateMapper morphGeneTemplateMapper = new("configurables/morph_gene_templates_map.txt");

	public DNAFactory(ModFilesystem irModFS, ModFilesystem ck3ModFS) {
		Logger.Debug("Reading color palettes...");
		var irHairPalettePath = irModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                        throw new ConverterException("Could not find Imperator hair palette!");
		irHairPalettePixels = new MagickImage(irHairPalettePath).GetPixelsUnsafe();
		var ck3HairPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 hair palette!");
		var ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixelsUnsafe();

		var irSkinPalettePath = irModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                        throw new ConverterException("Could not find Imperator skin palette!");
		irSkinPalettePixels = new MagickImage(irSkinPalettePath).GetPixelsUnsafe();
		var ck3SkinPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 skin palette!");
		var ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixelsUnsafe();

		var irEyePalettePath = irModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                       throw new ConverterException("Could not find Imperator eye palette!");
		irEyePalettePixels = new MagickImage(irEyePalettePath).GetPixelsUnsafe();
		var ck3EyePalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                        throw new ConverterException("Could not find CK3 eye palette!");
		var ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixelsUnsafe();
		
		Logger.Debug("Initializing genes database...");
		ck3GenesDB = new GenesDB(ck3ModFS);
		
		Logger.Debug("Building color conversion caches...");
		BuildColorConversionCaches(ck3HairPalettePixels, ck3SkinPalettePixels, ck3EyePalettePixels);
	}
	
	public DNA GenerateDNA(Imperator.Characters.Character irCharacter, PortraitData irPortraitData) {
		var id = $"dna_{irCharacter.Id}";
		
		var colorAndMorphDNAValues = new Dictionary<string, string>();

		var hairCoordinates = GetPaletteCoordinates(
			irPortraitData.HairColorPaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		var hairCoordinates2 = GetPaletteCoordinates(
			irPortraitData.HairColor2PaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		var hairValue = $"{hairCoordinates.X} {hairCoordinates.Y} {hairCoordinates2.X} {hairCoordinates2.Y}";
		colorAndMorphDNAValues.Add("hair_color", hairValue);

		var skinCoordinates = GetPaletteCoordinates(
			irPortraitData.SkinColorPaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		var skinCoordinates2 = GetPaletteCoordinates(
			irPortraitData.SkinColor2PaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		var skinValue = $"{skinCoordinates.X} {skinCoordinates.Y} {skinCoordinates2.X} {skinCoordinates2.Y}";
		colorAndMorphDNAValues.Add("skin_color", skinValue);

		var eyeCoordinates = GetPaletteCoordinates(
			irPortraitData.EyeColorPaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		var eyeCoordinates2 = GetPaletteCoordinates(
			irPortraitData.EyeColor2PaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		var eyeValue = $"{eyeCoordinates.X} {eyeCoordinates.Y} {eyeCoordinates2.X} {eyeCoordinates2.Y}";
		colorAndMorphDNAValues.Add("eye_color", eyeValue);
		
		// Convert some accessory genes.
		var accessoryDNAValues = new Dictionary<string, AccessoryGeneValue>();
		
		var beardGeneValue = MatchAccessoryGeneValueByObject(
			irCharacter, 
			irPortraitData, 
			"beards",
			ck3GenesDB.SpecialAccessoryGenes["beards"]
		);
		if (beardGeneValue is not null) {
			accessoryDNAValues.Add("beards", beardGeneValue.Value);
		}

		var hairstylesGeneValue = MatchAccessoryGeneValueByObject(
			irCharacter,
			irPortraitData,
			"hairstyles",
			ck3GenesDB.SpecialAccessoryGenes["hairstyles"]
		);
		if (hairstylesGeneValue is not null) {
			accessoryDNAValues.Add("hairstyles", hairstylesGeneValue.Value);
		}

		var clothesGeneValue = MatchAccessoryGeneValueByTemplate(irCharacter, irPortraitData, "clothes");
		if (clothesGeneValue is not null) {
			accessoryDNAValues.Add("clothes", clothesGeneValue.Value);
		}

		var irMorphGenesWithDirectEquivalents = new[] {
			"gene_head_height", "gene_head_width", "gene_head_profile",
			"gene_head_top_height", "gene_head_top_width",
			"gene_forehead_height", "gene_forehead_width", "gene_forehead_angle", "gene_forehead_roundness",
			"gene_forehead_brow_height",
			"gene_neck_length", "gene_neck_width", 
			"gene_chin_forward", "gene_chin_height", "gene_chin_width",
			"gene_eye_distance", "gene_eye_height", "gene_eye_angle", "gene_eye_depth", "gene_eye_shut",
			"gene_mouth_width", "gene_mouth_forward", "gene_mouth_height",
			"gene_mouth_corner_height", "gene_mouth_corner_depth",
			"gene_mouth_lower_lip_size", "gene_mouth_upper_lip_size",
			"gene_jaw_forward", "gene_jaw_angle", "gene_jaw_height", "gene_jaw_width",
			"gene_bs_jaw_def",
			"complexion"
		};

		foreach (var geneName in irMorphGenesWithDirectEquivalents) {
			var irGeneData = irPortraitData.MorphGenesDict[geneName];
			var ck3Gene = ck3GenesDB.MorphGenes.First(g => g.Id == geneName);

			var ck3TemplateName = morphGeneTemplateMapper.GetCK3Template(geneName, irGeneData.TemplateName);
			if (ck3Gene.GeneTemplates.All(t => t.Id != ck3TemplateName)) {
				Logger.Warn($"Could not find template {ck3TemplateName} for gene {geneName} in CK3!");
				continue;
			}

			var ck3GeneTemplateRecessiveName = morphGeneTemplateMapper.GetCK3Template(geneName, irGeneData.TemplateRecessiveName);
			if (ck3Gene.GeneTemplates.All(t => t.Id != ck3GeneTemplateRecessiveName)) {
				Logger.Warn($"Could not find template {ck3GeneTemplateRecessiveName} for gene {geneName} in CK3!");
				continue;
			}

			var geneValueStr = $"{ck3TemplateName} {irGeneData.Value} {ck3GeneTemplateRecessiveName} {irGeneData.ValueRecessive}";
			colorAndMorphDNAValues.Add(geneName, geneValueStr);
		}

		var ageGeneValue = GetAgeGeneValue(irPortraitData);
		if (ageGeneValue is not null) {
			colorAndMorphDNAValues.Add("gene_age", ageGeneValue);
		}

		// TODO: convert eyebrows

		// Use middle values for the rest of the genes.
		var missingMorphGenes = ck3GenesDB.MorphGenes
			.Where(g => !colorAndMorphDNAValues.ContainsKey(g.Id));
		foreach (var gene in missingMorphGenes) {
			if (irCharacter.Id == 14) { // TODO: remove this
				Logger.Error(gene.Id);
			}
			var geneTemplates = gene.GeneTemplates
				.OrderBy(t => t.Index)
				.ToImmutableList();
			var visibleGeneTemplates = geneTemplates
				.Where(t => t.Visible)
				.ToImmutableList();
			var geneTemplatesToUse = visibleGeneTemplates.Count > 0 ? visibleGeneTemplates : geneTemplates;
			// Get middle gene template.
			var templateName = geneTemplatesToUse.ElementAt(geneTemplatesToUse.Count / 2).Id;
			var geneValue = $"\"{templateName}\" 128 \"{templateName}\" 128";
			colorAndMorphDNAValues.Add(gene.Id, geneValue);
		}

		var missingAccessoryGenes = ck3GenesDB.AccessoryGenes
			.Where(g => !colorAndMorphDNAValues.ContainsKey(g.Id));
		foreach (var gene in missingAccessoryGenes) {
			var geneTemplates = gene.GeneTemplates
				.OrderBy(t => t.Index)
				.ToImmutableList();
			// Get middle gene template.
			var templateName = geneTemplates.ElementAt(geneTemplates.Count / 2).Id;
			var geneValue = $"\"{templateName}\" 128 \"{templateName}\" 128";
			colorAndMorphDNAValues.Add(gene.Id, geneValue);
		}
		
		return new DNA(id, colorAndMorphDNAValues, accessoryDNAValues);
	}
	
	/// Returns CK3 gene value string after object-to-object matching
	/// (for example I:R male_beard_1 to CK3 male_beard_western_03).
	private AccessoryGeneValue? MatchAccessoryGeneValueByObject(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string imperatorGeneName,
		AccessoryGene ck3Gene
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(imperatorGeneName, out var geneInfo)) {
			return null;
		}

		var objectMappings = accessoryGeneMapper.ObjectToObjectMappings[imperatorGeneName];
		var convertedSetEntry = objectMappings[geneInfo.ObjectName];
		var ck3GeneTemplate = ck3Gene.GeneTemplates
			.First(t => t.AgeSexWeightBlocks[irCharacter.AgeSex].ContainsObject(convertedSetEntry));
		var convertedSetEntryRecessive = objectMappings[geneInfo.ObjectNameRecessive];
		var ck3GeneTemplateRecessive = ck3Gene.GeneTemplates
			.First(t => t.AgeSexWeightBlocks[irCharacter.AgeSex].ContainsObject(convertedSetEntryRecessive));

		var matchingPercentage = ck3GeneTemplate.AgeSexWeightBlocks[irCharacter.AgeSex]
			.GetMatchingPercentage(convertedSetEntry);
		var matchingPercentageRecessive = ck3GeneTemplateRecessive.AgeSexWeightBlocks[irCharacter.AgeSex]
			.GetMatchingPercentage(convertedSetEntryRecessive);
		byte intSliderValue = (byte)Math.Ceiling(matchingPercentage * 255);
		byte intSliderValueRecessive = (byte)Math.Ceiling(matchingPercentageRecessive * 255);

		return new AccessoryGeneValue {
			TemplateName = ck3GeneTemplate.Id,
			IntSliderValue = intSliderValue,
			TemplateRecessiveName = ck3GeneTemplateRecessive.Id,
			IntSliderValueRecessive = intSliderValueRecessive
		};
	}
	
	/// Returns CK3 gene value string after template-to-template matching
	/// (for example I:R roman_clothes to CK3 byzantine_low_nobility_clothes).
	private AccessoryGeneValue? MatchAccessoryGeneValueByTemplate(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string imperatorGeneName
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(imperatorGeneName, out var geneInfo)) {
			return null;
		}

		var templateMappings = accessoryGeneMapper.TemplateToTemplateMappings[imperatorGeneName];
		var ck3GeneTemplateName = templateMappings[geneInfo.GeneTemplate];
		var ck3GeneTemplateNameRecessive = templateMappings[geneInfo.GeneTemplateRecessive];
		var intSliderValue = (byte)(irCharacter.Id % 256);

		return new AccessoryGeneValue {
			TemplateName = ck3GeneTemplateName,
			IntSliderValue = intSliderValue,
			TemplateRecessiveName = ck3GeneTemplateNameRecessive,
			IntSliderValueRecessive = intSliderValue
		};
	}

	private void BuildColorConversionCaches(
		IUnsafePixelCollection<ushort> ck3HairPalettePixels,
		IUnsafePixelCollection<ushort> ck3SkinPalettePixels,
		IUnsafePixelCollection<ushort> ck3EyePalettePixels
	) {
		BuildColorConversionCache(ck3HairPalettePixels, ck3HairColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3SkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3EyePalettePixels, ck3EyeColorToPaletteCoordinatesDict);
	}

	private static void BuildColorConversionCache(
		IUnsafePixelCollection<ushort> ck3PalettePixels,
		IDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		foreach (var pixel in ck3PalettePixels) {
			var color = pixel.ToColor();
			if (color is null) {
				continue;
			}

			var coordinates = new DNA.PaletteCoordinates { X = pixel.X, Y = pixel.Y };
			ck3ColorToCoordinatesDict[color] = coordinates;
		}
	}

	private static DNA.PaletteCoordinates GetPaletteCoordinates(
		PaletteCoordinates irPaletteCoordinates,
		IUnsafePixelCollection<ushort> irPalettePixels,
		IDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		var irColor = irPalettePixels.GetPixel(irPaletteCoordinates.X, irPaletteCoordinates.Y).ToColor();
		if (irColor is null) {
			Logger.Warn($"Cannot get color from palette {irPalettePixels}!");
			return new DNA.PaletteCoordinates();
		}
		
		if (ck3ColorToCoordinatesDict.TryGetValue(irColor, out var foundCoordinates)) {
			return foundCoordinates;
		}

		// Find the closest color in the CK3 palette with the 3D distance formula.
		var closestPair = ck3ColorToCoordinatesDict
			.MinBy(d => Math.Pow(d.Key.R - irColor.R, 2) + Math.Pow(d.Key.G - irColor.G, 2) + Math.Pow(d.Key.B - irColor.B, 2));
		var closestColorCoordinates = closestPair.Value;
		ck3ColorToCoordinatesDict[irColor] = closestColorCoordinates;
		return closestColorCoordinates;
	}

	private string? GetAgeGeneValue(PortraitData irPortraitData) {
		const string irAgeGeneName = "age";
		const string ck3AgeGeneName = "gene_age";
		
		if (!irPortraitData.MorphGenesDict.TryGetValue(irAgeGeneName, out var irGeneData)) {
			return null;
		}
		var ck3Gene = ck3GenesDB.MorphGenes.First(g => g.Id == ck3AgeGeneName);

		var ck3TemplateName = morphGeneTemplateMapper.GetCK3Template(irAgeGeneName, irGeneData.TemplateName);
		if (ck3Gene.GeneTemplates.All(t => t.Id != ck3TemplateName)) {
			Logger.Warn($"Could not find template {ck3TemplateName} for gene {ck3AgeGeneName} in CK3!");
			return null;
		}

		var ck3GeneTemplateRecessiveName = morphGeneTemplateMapper.GetCK3Template(irAgeGeneName, irGeneData.TemplateRecessiveName);
		if (ck3Gene.GeneTemplates.All(t => t.Id != ck3GeneTemplateRecessiveName)) {
			Logger.Warn($"Could not find template {ck3GeneTemplateRecessiveName} for gene {ck3AgeGeneName} in CK3!");
			return null;
		}

		return $"{ck3TemplateName} {irGeneData.Value} {ck3GeneTemplateRecessiveName} {irGeneData.ValueRecessive}";
	}
}