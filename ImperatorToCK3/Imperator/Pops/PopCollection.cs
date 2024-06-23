using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ImperatorToCK3.Imperator.Pops;

public sealed class PopCollection : ConcurrentIdObjectCollection<ulong, Pop> {
	public void LoadPopsFromBloc(BufferedReader blocReader) {
		var blocParser = new Parser();
		blocParser.RegisterKeyword("population", LoadPops);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(blocReader);
	}

	public void LoadPops(BufferedReader reader) {
		// Load pops using the producer-consumer pattern.
		
		var channel = Channel.CreateUnbounded<KeyValuePair<string, string>>();
		var channelWriter = channel.Writer;
		var channelReader = channel.Reader;
		
		var producerTask = Task.Run(() => {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.Integer, (popReader, thePopId) => {
				var popStr = popReader.GetStringOfItem().ToString();
				if (!popStr.Contains('{')) {
					return;
				}
				
				if (!channelWriter.TryWrite(new(thePopId, popStr))) {
					Logger.Warn($"Failed to enqueue pop {thePopId} for processing.");
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			
			channelWriter.Complete();
		});
		
		var consumerTasks = new List<Task>();
		for (var i = 0; i < 5; ++i) {
			consumerTasks.Add(Task.Run(async () => {
				await foreach (var (popIdStr, popDataStr) in channelReader.ReadAllAsync()) {
					var pop = Pop.Parse(popIdStr, new BufferedReader(popDataStr));
					Add(pop);
				}
			}));
		}
		
		Task.WaitAll(producerTask, Task.WhenAll(consumerTasks));
	}
}