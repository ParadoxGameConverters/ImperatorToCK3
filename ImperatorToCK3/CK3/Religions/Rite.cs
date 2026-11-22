using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

/// <summary>
/// A Rite type from CK3 1.20+. Rites are the lowest level of the religious hierarchy:
/// a religion contains faiths, and a faith contains rites.
/// </summary>
internal sealed class Rite : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public string? FaithId { get; private set; }
	public Color? Color { get; private set; }
	public OrderedSet<string> DoctrineIds { get; } = [];

	private readonly ReligionCollection religionCollection;
	private readonly OrderedDictionary<string, StringOfItem> localization = [];
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = [];

	public Rite(
		string id,
		BufferedReader riteReader,
		ReligionCollection religionCollection,
		ColorFactory colorFactory
	) {
		Id = id;
		this.religionCollection = religionCollection;

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("faith", reader => FaithId = reader.GetString());
		parser.RegisterKeyword("color", reader => {
			try {
				Color = colorFactory.GetColor(reader);
			} catch (System.Exception e) {
				Logger.Warn($"Found invalid color in rite {Id}! {e.Message}");
			}
		});
		parser.RegisterKeyword("tenets", reader => DoctrineIds.UnionWith(reader.GetStrings()));
		parser.RegisterKeyword("doctrines", reader => DoctrineIds.UnionWith(reader.GetStrings()));
		parser.RegisterKeyword("localization", reader => {
			var localizationParser = new Parser(implicitVariableHandling: true);
			localizationParser.RegisterRegex(CommonRegexes.Catchall, (locReader, locKey) => {
				localization[locKey] = locReader.GetStringOfItem();
			});
			localizationParser.ParseStream(reader);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		parser.ParseStream(riteReader);
	}

	private Rite(string id, string faithId, Color? color, IEnumerable<string> doctrineIds, ReligionCollection religionCollection) {
		Id = id;
		FaithId = faithId;
		Color = color;
		DoctrineIds = doctrineIds.ToOrderedSet();
		this.religionCollection = religionCollection;
	}

	/// <summary>
	/// Creates a rite matching a faith, like the mainline rites in the vanilla game.
	/// The game would otherwise generate a dynamic rite with an unreferenceable key for the faith.
	/// </summary>
	public static Rite CreateMainlineRite(Faith faith) {
		return new Rite(faith.Id, faith.Id, faith.Color, faith.DoctrineIds, faith.Religion.ReligionCollection);
	}

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		if (FaithId is not null) {
			sb.Append(contentIndent).AppendLine($"faith = {FaithId}");
		}
		if (Color is not null) {
			sb.Append(contentIndent).AppendLine($"color = {Color.Value.OutputRgb()}");
		}

		Faith.AppendDoctrinesAndTenets(sb, contentIndent, DoctrineIds, religionCollection.IsTenet);

		sb.Append(contentIndent).AppendLine("localization = {");
		foreach (var (key, value) in localization) {
			sb.Append(contentIndent).Append('\t').AppendLine($"{key}={value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}
