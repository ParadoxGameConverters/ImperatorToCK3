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
	private readonly IPixelCollection<ushort> irHairPalettePixels;
	private readonly IPixelCollection<ushort> irSkinPalettePixels;
	private readonly IPixelCollection<ushort> irEyePalettePixels;

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
		irHairPalettePixels = new MagickImage(irHairPalettePath).GetPixels();
		var ck3HairPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 hair palette!");
		var ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixels();

		var irSkinPalettePath = irModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                        throw new ConverterException("Could not find Imperator skin palette!");
		irSkinPalettePixels = new MagickImage(irSkinPalettePath).GetPixels();
		var ck3SkinPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 skin palette!");
		var ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixels();

		var irEyePalettePath = irModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                       throw new ConverterException("Could not find Imperator eye palette!");
		irEyePalettePixels = new MagickImage(irEyePalettePath).GetPixels();
		var ck3EyePalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                        throw new ConverterException("Could not find CK3 eye palette!");
		var ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixels();
		
		Logger.Debug("Initializing genes database...");
		ck3GenesDB = new GenesDB(ck3ModFS);
		
		Logger.Debug("Building color conversion caches...");
		BuildColorConversionCaches(ck3HairPalettePixels, ck3SkinPalettePixels, ck3EyePalettePixels);
	}
	
	public DNA GenerateDNA(Imperator.Characters.Character irCharacter, PortraitData irPortraitData) {
		var id = $"dna_{irCharacter.Id}";

		var colorDNAValues = new Dictionary<string, DNAColorGeneValue>();
		var morphDNAValues = new Dictionary<string, DNAGeneValue>();

		// Convert colors. Palettes are 512x512, but we need a 0-255 value, so we divide the coordinates by 2.
		var hairCoordinates = GetPaletteCoordinates(
			irPortraitData.HairColorPaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		var hairCoordinates2 = GetPaletteCoordinates(
			irPortraitData.HairColor2PaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		colorDNAValues.Add("hair_color", new DNAColorGeneValue {
			X = (byte)(hairCoordinates.X/2),
			Y = (byte)(hairCoordinates.Y/2),
			XRecessive = (byte)(hairCoordinates2.X/2),
			YRecessive = (byte)(hairCoordinates2.Y/2)
		});

		var skinCoordinates = GetPaletteCoordinates(
			irPortraitData.SkinColorPaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		var skinCoordinates2 = GetPaletteCoordinates(
			irPortraitData.SkinColor2PaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		colorDNAValues.Add("skin_color", new DNAColorGeneValue {
			X = (byte)(skinCoordinates.X/2),
			Y = (byte)(skinCoordinates.Y/2),
			XRecessive = (byte)(skinCoordinates2.X/2),
			YRecessive = (byte)(skinCoordinates2.Y/2)
		});

		var eyeCoordinates = GetPaletteCoordinates(
			irPortraitData.EyeColorPaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		var eyeCoordinates2 = GetPaletteCoordinates(
			irPortraitData.EyeColor2PaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		colorDNAValues.Add("eye_color", new DNAColorGeneValue {
			X = (byte)(eyeCoordinates.X/2),
			Y = (byte)(eyeCoordinates.Y/2),
			XRecessive = (byte)(eyeCoordinates2.X/2),
			YRecessive = (byte)(eyeCoordinates2.Y/2)
		});
		
		// Convert some accessory genes.
		var accessoryDNAValues = new Dictionary<string, DNAGeneValue>();
		
		if (ck3GenesDB.SpecialAccessoryGenes.TryGetValue("beards", out var beardGene)) {
			var beardGeneValue = MatchAccessoryGeneValueByObject(
				irCharacter, 
				irPortraitData, 
				"beards",
				beardGene
			);
			if (beardGeneValue is not null) {
				accessoryDNAValues.Add("beards", beardGeneValue.Value);
			}
		} else {
			Logger.Warn("beards not found in CK3 special accessory genes!");
		}

		if (ck3GenesDB.SpecialAccessoryGenes.TryGetValue("hairstyles", out var hairstylesGene)) {
			var hairstylesGeneValue = MatchAccessoryGeneValueByObject(
				irCharacter,
				irPortraitData,
				"hairstyles",
				hairstylesGene
			);
			if (hairstylesGeneValue is not null) {
				accessoryDNAValues.Add("hairstyles", hairstylesGeneValue.Value);
			}
		} else {
			Logger.Warn("hairstyles not found in CK3 special accessory genes!");
		}

		var clothesGeneValue = MatchAccessoryGeneValueByTemplate(irCharacter, irPortraitData, "clothes");
		if (clothesGeneValue is not null) {
			accessoryDNAValues.Add("clothes", clothesGeneValue.Value);
		}
		
		// Convert eye accessories.
		var irEyeAccessoryGeneTemplateName = irPortraitData.AccessoryGenesDict["eye_accessory"].GeneTemplate;
		switch (irEyeAccessoryGeneTemplateName) {
			case "normal_eyes":
				break;
			case "eyepatch_1":
			case "eyepatch_2":
				accessoryDNAValues["special_headgear_eye_patch"] = new DNAGeneValue {
					TemplateName = "eye_patch",
					IntSliderValue = 255,
					TemplateRecessiveName = "eye_patch",
					IntSliderValueRecessive = 255
				};
				break;
			case "blindfold_1":
				accessoryDNAValues["special_headgear_blindfold"] = new DNAGeneValue {
					TemplateName = "blindfold",
					IntSliderValue = 255,
					TemplateRecessiveName = "blindfold",
					IntSliderValueRecessive = 255
				};
				break;
			case "blind_eyes":
				accessoryDNAValues["eye_accessory"] = new DNAGeneValue {
					TemplateName = "blind_eyes",
					IntSliderValue = 127,
					TemplateRecessiveName = "blind_eyes",
					IntSliderValueRecessive = 0
				};
				break;
			case "red_eyes":
				var magickRed = new MagickColor("#ff0000");
				var redEyeCoordinates = GetCoordinatesOfClosestCK3Color(magickRed, ck3EyeColorToPaletteCoordinatesDict);
				colorDNAValues["eye_color"] = colorDNAValues["eye_color"] with {
					X = (byte)(redEyeCoordinates.X/2),
					Y = (byte)(redEyeCoordinates.Y/2)
				};
				break;
			default:
				Logger.Warn($"Unhandled eye accessory gene template name: {irEyeAccessoryGeneTemplateName}");
				break;
		}
		if (irCharacter.Traits.Contains("blind")) {
			accessoryDNAValues["eye_accessory"] = new DNAGeneValue {
				TemplateName = "blind_eyes",
				IntSliderValue = 127,
				TemplateRecessiveName = "blind_eyes",
				IntSliderValueRecessive = 0
			};
			accessoryDNAValues["special_headgear_blindfold"] = new DNAGeneValue {
				TemplateName = "blindfold",
				IntSliderValue = 255,
				TemplateRecessiveName = "blindfold",
				IntSliderValueRecessive = 255
			};
		} else if (irCharacter.Traits.Contains("one_eyed")) {
			accessoryDNAValues["special_headgear_eye_patch"] = new DNAGeneValue {
				TemplateName = "eye_patch",
				IntSliderValue = 255,
				TemplateRecessiveName = "eye_patch",
				IntSliderValueRecessive = 255
			};
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

			morphDNAValues.Add(geneName, new DNAGeneValue {
				TemplateName = ck3TemplateName!,
				IntSliderValue = irGeneData.Value,
				TemplateRecessiveName = ck3GeneTemplateRecessiveName!,
				IntSliderValueRecessive = irGeneData.ValueRecessive
			});
		}
		
		morphDNAValues.Add("gene_age", GetAgeGeneValue(irCharacter));

		// Convert baldness.
		if (irCharacter.IsBald) {
			morphDNAValues["gene_baldness"] = new DNAGeneValue {
				TemplateName = "male_pattern_baldness",
				IntSliderValue = 255,
				TemplateRecessiveName = "male_pattern_baldness",
				IntSliderValueRecessive = 127
			};
			// CK3 does not seem to actually support baldness (as of CK3 1.8.1) despite the gene being there.
			// So we just remove the hair.
			if (accessoryDNAValues.TryGetValue("hairstyles", out var hairstylesGeneValue)) {
				accessoryDNAValues["hairstyles"] = hairstylesGeneValue with {
					TemplateName = "no_hairstyles", IntSliderValue = 0
				};
			}
		} else {
			morphDNAValues["gene_baldness"] = new DNAGeneValue {
				TemplateName = "no_baldness",
				IntSliderValue = 127,
				TemplateRecessiveName = "no_baldness",
				IntSliderValueRecessive = 127
			};
		}

		// Use middle values for the rest of the genes.
		var missingMorphGenes = ck3GenesDB.MorphGenes
			.Where(g => !morphDNAValues.ContainsKey(g.Id));
		foreach (var gene in missingMorphGenes) {
			var geneTemplates = gene.GeneTemplates
				.OrderBy(t => t.Index)
				.ToImmutableList();
			var visibleGeneTemplates = geneTemplates
				.Where(t => t.Visible)
				.ToImmutableList();
			var geneTemplatesToUse = visibleGeneTemplates.Count > 0 ? visibleGeneTemplates : geneTemplates;
			// Get middle gene template.
			var templateName = geneTemplatesToUse.ElementAt(geneTemplatesToUse.Count / 2).Id;
			morphDNAValues.Add(gene.Id, new DNAGeneValue {
				TemplateName = templateName,
				IntSliderValue = 128,
				TemplateRecessiveName = templateName,
				IntSliderValueRecessive = 128
			});
		}

		var missingAccessoryGenes = ck3GenesDB.AccessoryGenes
			.Where(g => !accessoryDNAValues.ContainsKey(g.Id));
		foreach (var gene in missingAccessoryGenes) {
			var geneTemplates = gene.GeneTemplates
				.OrderBy(t => t.Index)
				.ToImmutableList();
			// Get middle gene template.
			var templateName = geneTemplates.ElementAt(geneTemplates.Count / 2).Id;
			accessoryDNAValues.Add(gene.Id, new DNAGeneValue {
				TemplateName = templateName,
				IntSliderValue = 128,
				TemplateRecessiveName = templateName,
				IntSliderValueRecessive = 128
			});
		}
		
		return new DNA(id, colorDNAValues, morphDNAValues, accessoryDNAValues);
	}
	
	/// Returns CK3 gene value string after object-to-object matching
	/// (for example I:R male_beard_1 to CK3 male_beard_western_03).
	private DNAGeneValue? MatchAccessoryGeneValueByObject(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string irGeneName,
		AccessoryGene ck3Gene
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(irGeneName, out var geneInfo)) {
			return null;
		}

		var objectMappings = accessoryGeneMapper.ObjectToObjectMappings[irGeneName];
		if (!objectMappings.TryGetValue(geneInfo.ObjectName, out var convertedSetEntry)) {
			Logger.Warn($"No object mappings found for {geneInfo.ObjectName} in gene {irGeneName}!");
			return null;
		}
		var ck3GeneTemplate = ck3Gene.GeneTemplates
			.First(t => t.AgeSexWeightBlocks[irCharacter.AgeSex].ContainsObject(convertedSetEntry));
		if (!objectMappings.TryGetValue(geneInfo.ObjectNameRecessive, out var convertedSetEntryRecessive)) {
			Logger.Warn($"No object mappings found for {geneInfo.ObjectNameRecessive} in gene {irGeneName}!");
			return null;
		}
		var ck3GeneTemplateRecessive = ck3Gene.GeneTemplates
			.First(t => t.AgeSexWeightBlocks[irCharacter.AgeSex].ContainsObject(convertedSetEntryRecessive));

		var matchingPercentage = ck3GeneTemplate.AgeSexWeightBlocks[irCharacter.AgeSex]
			.GetMatchingPercentage(convertedSetEntry);
		var matchingPercentageRecessive = ck3GeneTemplateRecessive.AgeSexWeightBlocks[irCharacter.AgeSex]
			.GetMatchingPercentage(convertedSetEntryRecessive);
		byte intSliderValue = (byte)Math.Ceiling(matchingPercentage * 255);
		byte intSliderValueRecessive = (byte)Math.Ceiling(matchingPercentageRecessive * 255);

		return new DNAGeneValue {
			TemplateName = ck3GeneTemplate.Id,
			IntSliderValue = intSliderValue,
			TemplateRecessiveName = ck3GeneTemplateRecessive.Id,
			IntSliderValueRecessive = intSliderValueRecessive
		};
	}
	
	/// Returns CK3 gene value string after template-to-template matching
	/// (for example I:R roman_clothes to CK3 byzantine_low_nobility_clothes).
	private DNAGeneValue? MatchAccessoryGeneValueByTemplate(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string imperatorGeneName
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(imperatorGeneName, out var geneInfo)) {
			return null;
		}

		if (!accessoryGeneMapper.TemplateToTemplateMappings.TryGetValue(imperatorGeneName, out var templateMappings)) {
			Logger.Warn($"No template-to-template mappings found for gene {imperatorGeneName}!");
			return null;
		}
		if (!templateMappings.TryGetValue(geneInfo.GeneTemplate, out var ck3GeneTemplateName)) {
			Logger.Warn($"No template-to-template mapping found for gene {imperatorGeneName} and template {geneInfo.GeneTemplate}!");
			// Try to return first found template as a fallback.
			if (templateMappings.Count > 0) {
				ck3GeneTemplateName = templateMappings.First().Value;
			} else {
				return null;
			}
		}
		if (!templateMappings.TryGetValue(geneInfo.GeneTemplateRecessive, out var ck3GeneTemplateNameRecessive)) {
			Logger.Warn($"No template-to-template mapping found for gene {imperatorGeneName} and recessive template {geneInfo.GeneTemplateRecessive}!");
			// Use dominant template as a fallback.
			ck3GeneTemplateNameRecessive = ck3GeneTemplateName;
		}
		var intSliderValue = (byte)(irCharacter.Id % 256);

		return new DNAGeneValue {
			TemplateName = ck3GeneTemplateName,
			IntSliderValue = intSliderValue,
			TemplateRecessiveName = ck3GeneTemplateNameRecessive,
			IntSliderValueRecessive = intSliderValue
		};
	}

	private void BuildColorConversionCaches(
		IPixelCollection<ushort> ck3HairPalettePixels,
		IPixelCollection<ushort> ck3SkinPalettePixels,
		IPixelCollection<ushort> ck3EyePalettePixels
	) {
		BuildColorConversionCache(ck3HairPalettePixels, ck3HairColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3SkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3EyePalettePixels, ck3EyeColorToPaletteCoordinatesDict);
	}

	private static void BuildColorConversionCache(
		IPixelCollection<ushort> ck3PalettePixels,
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
		IPixelCollection<ushort> irPalettePixels,
		IDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		var irColor = irPalettePixels.GetPixel(irPaletteCoordinates.X, irPaletteCoordinates.Y).ToColor();
		if (irColor is not null) {
			return GetCoordinatesOfClosestCK3Color(irColor, ck3ColorToCoordinatesDict);
		}

		Logger.Warn($"Cannot get color from palette {irPalettePixels}!");
		return new DNA.PaletteCoordinates();

	}
	
	private static DNA.PaletteCoordinates GetCoordinatesOfClosestCK3Color(
		IMagickColor<ushort> irColor,
		IDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
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

	private DNAGeneValue GetAgeGeneValue(Imperator.Characters.Character irCharacter) {
		// Age is not stored in I:R character DNA.
		const string ck3AgeGeneName = "gene_age";
		var ck3Gene = ck3GenesDB.MorphGenes.First(g => g.Id == ck3AgeGeneName);
		var excludedAgeTemplateNames = new List<string> {"old_beauty_1", "no_aging"};
		var possibleAgeTemplates = ck3Gene.GeneTemplates
			.Where(t => !excludedAgeTemplateNames.Contains(t.Id))
			.ToList();
		var selectedTemplateName = possibleAgeTemplates[(int)(irCharacter.Id % (ulong)possibleAgeTemplates.Count)].Id;
		var selectedTemplateRecessiveName = possibleAgeTemplates[(int)(irCharacter.Age % possibleAgeTemplates.Count)].Id;

		return new DNAGeneValue {
			TemplateName = selectedTemplateName,
			IntSliderValue = 128,
			TemplateRecessiveName = selectedTemplateRecessiveName,
			IntSliderValueRecessive = 128
		};
	}
}