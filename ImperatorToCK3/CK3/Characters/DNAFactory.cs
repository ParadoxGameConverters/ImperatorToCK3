using commonItems;
using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters; 

public sealed class DNAFactory {
	private readonly IPixelCollection<ushort> irHairPalettePixels;
	private readonly IPixelCollection<ushort> irSkinPalettePixels;
	private readonly IPixelCollection<ushort> irEyePalettePixels;

	private readonly ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3HairColorToPaletteCoordinatesDict = new();
	private readonly ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3SkinColorToPaletteCoordinatesDict = new();
	private readonly ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3EyeColorToPaletteCoordinatesDict = new();
	
	private readonly GenesDB ck3GenesDB;
	private readonly AccessoryGeneMapper accessoryGeneMapper = new("configurables/accessory_genes_map.txt");
	private readonly MorphGeneTemplateMapper morphGeneTemplateMapper = new("configurables/morph_gene_templates_map.txt");

	public DNAFactory(ModFilesystem irModFS, ModFilesystem ck3ModFS) {
		Logger.Debug("Reading color palettes...");
		
		var ck3HairPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 hair palette!");
		var ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixels();
		var irHairPalettePath = irModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds");
		if (irHairPalettePath is null) {
			Logger.Warn("Could not find Imperator hair palette, using CK3 palette as fallback!");
			irHairPalettePixels = ck3HairPalettePixels;
		} else {
			irHairPalettePixels = new MagickImage(irHairPalettePath).GetPixels();
		}

		var ck3SkinPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 skin palette!");
		var ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixels();
		var irSkinPalettePath = irModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds");
		if (irSkinPalettePath is null) {
			Logger.Warn("Could not find Imperator skin palette, using CK3 palette as fallback!");
			irSkinPalettePixels = ck3SkinPalettePixels;
		} else {
			irSkinPalettePixels = new MagickImage(irSkinPalettePath).GetPixels();
		}

		var ck3EyePalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                        throw new ConverterException("Could not find CK3 eye palette!");
		var ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixels();
		var irEyePalettePath = irModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds");
		if (irEyePalettePath is null) {
			Logger.Warn("Could not find Imperator eye palette, using CK3 palette as fallback!");
			irEyePalettePixels = ck3EyePalettePixels;
		} else {
			irEyePalettePixels = new MagickImage(irEyePalettePath).GetPixels();
		}
		
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
		var accessoryDNAValues = new Dictionary<string, DNAAccessoryGeneValue>();
		
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
		
		if (ck3GenesDB.SpecialAccessoryGenes.TryGetValue("clothes", out var ck3ClothesGene)) {
			var clothesGeneValue = MatchAccessoryGeneValueByTemplate(irCharacter, irPortraitData, "clothes", ck3ClothesGene);
			if (clothesGeneValue is not null) {
				accessoryDNAValues.Add(ck3ClothesGene.Id, clothesGeneValue.Value);
			}
		} else {
			Logger.Warn("clothes not found in CK3 special accessory genes!");
		}
		
		// Convert eye accessories.
		var irEyeAccessoryGeneTemplateName = irPortraitData.AccessoryGenesDict["eye_accessory"].GeneTemplate;
		switch (irEyeAccessoryGeneTemplateName) {
			case "normal_eyes":
				break;
			case "eyepatch_1":
			case "eyepatch_2": // TODO: check if this is correctly added to portrait modifiers if needed
				var eyePatchTemplate = ck3GenesDB.SpecialAccessoryGenes["special_headgear_eye_patch"]
					.GeneTemplates["eye_patch"];
				if (eyePatchTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? eyePatchWeightBlock)) {
					var eyePatchObjectName = eyePatchWeightBlock.GetMatchingObject(1) ?? eyePatchWeightBlock.ObjectNames.Last();
					accessoryDNAValues["special_headgear_eye_patch"] = new(eyePatchTemplate.Id, eyePatchObjectName, eyePatchWeightBlock);
				}

				break;
			case "blindfold_1": // TODO: check if this is correctly added to portrait modifiers if needed
				var blindfoldTemplate = ck3GenesDB.SpecialAccessoryGenes["special_headgear_blindfold"]
					.GeneTemplates["blindfold"];
				if (blindfoldTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? blindfoldWeightBlock)) {
					var blindfoldObjectName = blindfoldWeightBlock.GetMatchingObject(1) ?? blindfoldWeightBlock.ObjectNames.Last();
					accessoryDNAValues["special_headgear_blindfold"] = new(blindfoldTemplate.Id, blindfoldObjectName, blindfoldWeightBlock);
				}

				break;
			case "blind_eyes": // TODO: check if this is correctly added to portrait modifiers if needed
				var blindEyesTemplate = ck3GenesDB.AccessoryGenes["eye_accessory"]
					.GeneTemplates["blind_eyes"];
				if (blindEyesTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? blindEyesWeightBlock)) {
					var blindEyesObjectName = blindEyesWeightBlock.GetMatchingObject(1) ?? blindEyesWeightBlock.ObjectNames.Last();
					accessoryDNAValues["eye_accessory"] = new(blindEyesTemplate.Id, blindEyesObjectName, blindEyesWeightBlock);
				}

				break;
			case "red_eyes": // TODO: check if this is correctly converted
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
			var blindEyesTemplate = ck3GenesDB.AccessoryGenes["eye_accessory"].GeneTemplates["blind_eyes"];
			if (blindEyesTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? blindEyesWeighBlock)) {
				var blindEyesObjectName = blindEyesWeighBlock.GetMatchingObject(1) ?? blindEyesWeighBlock.ObjectNames.Last();
				accessoryDNAValues["eye_accessory"] = new(blindEyesTemplate.Id, blindEyesObjectName, blindEyesWeighBlock); // TODO: check if this is correctly added to portrait modifiers if needed
			}

			var blindfoldTemplate = ck3GenesDB.SpecialAccessoryGenes["special_headgear_blindfold"]
				.GeneTemplates["blindfold"];
			if (blindfoldTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? blindfoldWeighBlock)) {
				var blindfoldObjectName = blindfoldWeighBlock.GetMatchingObject(1) ?? blindfoldWeighBlock.ObjectNames.Last();
				accessoryDNAValues["special_headgear_blindfold"] = new(blindfoldTemplate.Id, blindfoldObjectName, blindfoldWeighBlock); // TODO: check if this is correctly added to portrait modifiers if needed
			}
		} else if (irCharacter.Traits.Contains("one_eyed")) {
			var eyePatchTemplate = ck3GenesDB.SpecialAccessoryGenes["special_headgear_eye_patch"]
				.GeneTemplates["eye_patch"];
			if (eyePatchTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? eyePatchWeighBlock)) {
				var eyePatchObjectName = eyePatchWeighBlock.GetMatchingObject(1) ?? eyePatchWeighBlock.ObjectNames.Last();
				accessoryDNAValues["special_headgear_eye_patch"] = new(eyePatchTemplate.Id, eyePatchObjectName, eyePatchWeighBlock); // TODO: check if this is correctly added to portrait modifiers if needed
			}
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

		ConvertBaldness(irCharacter, morphDNAValues, accessoryDNAValues);
		
		// Use normal teeth for everyone. I:R doesn't have characters with no teeth.
		var teethGeneTemplate = ck3GenesDB.AccessoryGenes["teeth_accessory"].GeneTemplates["normal_teeth"];
		if (teethGeneTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? teethWeightBlock)) {
			accessoryDNAValues["teeth_accessory"] = new DNAAccessoryGeneValue(
				teethGeneTemplate.Id,
				teethWeightBlock.GetMatchingObject(0.5) ?? teethWeightBlock.ObjectNames.First(),
				teethWeightBlock
			);
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
			var middleTemplate = geneTemplates.ElementAt(geneTemplates.Count / 2);
			if (middleTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? weightBlock)) {
				var middleObjectName = weightBlock.GetMatchingObject(0.5);
				if (middleObjectName is not null) {
					accessoryDNAValues[gene.Id] = new(middleTemplate.Id, middleObjectName, weightBlock);
				} else {
					Logger.Warn($"Failed to find middle object for gene {gene.Id}!");
				}
			}
		}

		return new DNA(id, colorDNAValues, morphDNAValues, accessoryDNAValues);
	}

	private void ConvertBaldness(Imperator.Characters.Character irCharacter, Dictionary<string, DNAGeneValue> morphDNAValues, Dictionary<string, DNAAccessoryGeneValue> accessoryDNAValues) {
		if (irCharacter.IsBald) {  // TODO: CHECK IF BALD CHARACTERS STILL CORRECTLY APPEAR BALD IN CK3
			morphDNAValues["gene_baldness"] = new DNAGeneValue {
				TemplateName = "male_pattern_baldness",
				IntSliderValue = 127,
				TemplateRecessiveName = "male_pattern_baldness",
				IntSliderValueRecessive = 127
			};
			
			if (ck3GenesDB.SpecialAccessoryGenes.TryGetValue("hairstyles", out var hairstylesGene)) {
				DNAAccessoryGeneValue? hairstylesGeneValue = null;
				
				// If m_hair_fp4_indian_01_full_bald (which is close to I:R baldness) exists, use it.
				const string indianBaldnessObjectName = "m_hair_fp4_indian_01_full_bald";
				if (hairstylesGene.GeneTemplates.TryGetValue("fp4_bald_hairstyles", out var ck3GeneTemplate)) {
					if (ck3GeneTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? weightBlock) && weightBlock.ContainsObject(indianBaldnessObjectName)) {
						hairstylesGeneValue = new DNAAccessoryGeneValue(ck3GeneTemplate.Id, indianBaldnessObjectName, weightBlock);
					}
				}
				
				// Otherwise, just use the no_hairstyles template.
				const string baldnessObjectName = "bald";
				if (hairstylesGeneValue is null && hairstylesGene.GeneTemplates.TryGetValue("no_hairstyles", out var noHairStylesTemplate)) {
					if (noHairStylesTemplate.AgeSexWeightBlocks.TryGetValue(irCharacter.AgeSex, out WeightBlock? weightBlock) && weightBlock.ContainsObject(baldnessObjectName)) {
						hairstylesGeneValue = new DNAAccessoryGeneValue(noHairStylesTemplate.Id, baldnessObjectName, weightBlock);
					}
				}
				
				if (hairstylesGeneValue.HasValue) {
					accessoryDNAValues["hairstyles"] = hairstylesGeneValue.Value;
				}
			}
			
			morphDNAValues["gene_balding_hair_effect"] = new DNAGeneValue {
				TemplateName = "baldness_stage_2",
				IntSliderValue = 255,
				TemplateRecessiveName = "baldness_stage_2",
				IntSliderValueRecessive = 255
			};
		} else {
			morphDNAValues["gene_baldness"] = new DNAGeneValue {
				TemplateName = "no_baldness",
				IntSliderValue = 127,
				TemplateRecessiveName = "no_baldness",
				IntSliderValueRecessive = 127
			};
		}
	}

	/// Returns CK3 gene value string after object-to-object matching
	/// (for example I:R male_beard_1 to CK3 male_beard_western_03).
	private DNAAccessoryGeneValue? MatchAccessoryGeneValueByObject(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string irGeneName,
		AccessoryGene ck3Gene
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(irGeneName, out var geneInfo)) {
			return null;
		}
		
		var convertedSetEntry = accessoryGeneMapper.GetObjectFromObject(irGeneName, geneInfo.ObjectName);
		if (convertedSetEntry is null) {
			Logger.Warn($"No object mappings found for {geneInfo.ObjectName} in gene {irGeneName}!");
			return null;
		}
		var ck3GeneTemplate = ck3Gene.GeneTemplates
			.FirstOrDefault(t => t.ContainsObjectForAgeSex(irCharacter.AgeSex, convertedSetEntry));
		if (ck3GeneTemplate is null) {
			Logger.Warn($"No template found for {convertedSetEntry} in CK3 gene {ck3Gene.Id}!");
			return null;
		}
		var convertedSetEntryRecessive = accessoryGeneMapper.GetObjectFromObject(irGeneName, geneInfo.ObjectNameRecessive);
		if (convertedSetEntryRecessive is null) {
			Logger.Warn($"No object mappings found for {geneInfo.ObjectNameRecessive} in gene {irGeneName}!");
			return null;
		}
		var ck3GeneTemplateRecessive = ck3Gene.GeneTemplates
			.FirstOrDefault(t => t.ContainsObjectForAgeSex(irCharacter.AgeSex, convertedSetEntryRecessive));
		if (ck3GeneTemplateRecessive is null) {
			Logger.Warn($"No template found for {convertedSetEntryRecessive} in CK3 gene {ck3Gene.Id}!");
			return null;
		}

		return new DNAAccessoryGeneValue(ck3GeneTemplate.Id, convertedSetEntry, ck3GeneTemplate.AgeSexWeightBlocks[irCharacter.AgeSex], ck3GeneTemplateRecessive.Id, convertedSetEntryRecessive, ck3GeneTemplateRecessive.AgeSexWeightBlocks[irCharacter.AgeSex]);
	}
	
	/// Returns CK3 gene value string after template-to-template matching
	/// (for example I:R roman_clothes to CK3 byzantine_low_nobility_clothes).
	private DNAAccessoryGeneValue? MatchAccessoryGeneValueByTemplate(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string imperatorGeneName,
		AccessoryGene ck3Gene
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(imperatorGeneName, out var geneInfo)) {
			return null;
		}
		
		var validCK3TemplateIds = ck3Gene.GeneTemplates
			.Select(template => template.Id)
			.ToList();

		var ck3GeneTemplateName = accessoryGeneMapper.GetTemplateFromTemplate(imperatorGeneName, geneInfo.GeneTemplate, validCK3TemplateIds);
		if (ck3GeneTemplateName is null) {
			Logger.Warn($"No template-to-template mapping found for gene {imperatorGeneName} and template {geneInfo.GeneTemplate}!");
			// Try to return first found template as a fallback.
			var fallbackTemplateName = accessoryGeneMapper.GetFallbackTemplateForGene(imperatorGeneName, validCK3TemplateIds);
			if (fallbackTemplateName is not null) {
				ck3GeneTemplateName = fallbackTemplateName;
			} else {
				return null;
			}
		}
		var ck3GeneTemplateNameRecessive = accessoryGeneMapper.GetTemplateFromTemplate(imperatorGeneName, geneInfo.GeneTemplateRecessive, validCK3TemplateIds);
		if (ck3GeneTemplateNameRecessive is null) {
			Logger.Warn($"No template-to-template mapping found for gene {imperatorGeneName} and recessive template {geneInfo.GeneTemplateRecessive}!");
			// Use dominant template as a fallback.
			ck3GeneTemplateNameRecessive = ck3GeneTemplateName;
		}
		double percentage = (irCharacter.Id % 100) / 100.0;
		
		var ck3GeneTemplate = ck3Gene.GeneTemplates.First(t => t.Id == ck3GeneTemplateName);
		var ck3WeightBlock = ck3GeneTemplate.AgeSexWeightBlocks[irCharacter.AgeSex];
		var ck3ObjectName = ck3WeightBlock.GetMatchingObject(percentage) ?? ck3WeightBlock.ObjectNames.First();
		
		var ck3GeneTemplateRecessive = ck3Gene.GeneTemplates.First(t => t.Id == ck3GeneTemplateNameRecessive);
		var ck3WeightBlockRecessive = ck3GeneTemplateRecessive.AgeSexWeightBlocks[irCharacter.AgeSex];
		var ck3ObjectNameRecessive = ck3WeightBlockRecessive.GetMatchingObject(percentage) ?? ck3WeightBlockRecessive.ObjectNames.First();
		
		return new DNAAccessoryGeneValue(ck3GeneTemplateName, ck3ObjectName, ck3WeightBlock, ck3GeneTemplateNameRecessive, ck3ObjectNameRecessive, ck3WeightBlockRecessive);
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
		ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
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
		ConcurrentDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
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