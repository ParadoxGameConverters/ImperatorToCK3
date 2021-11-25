using commonItems;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ImperatorToCK3.Imperator.Pops {
	public class Pops : IReadOnlyDictionary<ulong, Pop> {
		public void LoadPops(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, thePopId) => {
				var popStr = reader.GetStringOfItem().ToString();
				if (popStr.Contains('{')) {
					var tempStream = new BufferedReader(popStr);
					var pop = Pop.Parse(thePopId, tempStream);
					Add(pop);
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		public void Add(Pop pop) {
			popsDict.Add(pop.Id, pop);
		}
		
		public bool ContainsKey(ulong key) => popsDict.ContainsKey(key);
		public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out Pop value) => popsDict.TryGetValue(key, out value);
		public IEnumerator<KeyValuePair<ulong, Pop>> GetEnumerator() => popsDict.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => popsDict.GetEnumerator();
		public IEnumerable<ulong> Keys => popsDict.Keys;
		public IEnumerable<Pop> Values => popsDict.Values;
		public int Count => popsDict.Count;
		public Pop this[ulong key] => popsDict[key];
		private readonly Dictionary<ulong, Pop> popsDict = new();
	}
}
