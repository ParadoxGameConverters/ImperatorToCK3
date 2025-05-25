using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class DiplomacyOutputter {
	public static async Task OutputLeagues(string outputModPath, List<List<Title>> leagues) {
		var outputPath = Path.Combine(outputModPath, "common/on_action/irtock3_confederations.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);

		var sb = new StringBuilder();
		sb.AppendLine(
			"""
			on_game_start/on_game_start_after_lobby = {
				on_actions = {
					irtock3_confederation_setup
				}
			}

			irtock3_confederation_setup = {
				effect = {
			""");
		foreach (var leagueMembers in leagues) {
			// Make sure there are at least two members in the league.
			if (leagueMembers.Count < 2) {
				continue;
			}

			var leagueMemberIds = leagueMembers.ConvertAll(member => member.Id);
			WriteEffectsForLeague(sb, leagueMemberIds);
		}

		sb.AppendLine(
			"""
				}
			}
			""");

		await output.WriteAsync(sb.ToString());
	}

	private static void WriteEffectsForLeague(StringBuilder sb, List<string> leagueMemberIds) {
		var firstMemberId = leagueMemberIds[0];
		var secondMemberId = leagueMemberIds[1];
		sb.AppendLine(
			$$"""
			  		title:{{firstMemberId}}.holder = {
			  			add_to_list = irtock3_confederation_members
			  			save_scope_as = first_confed_member
			  		}
			  		title:{{secondMemberId}}.holder = {
			  			add_to_list = irtock3_confederation_members
			  			save_scope_as = second_confed_member
			  		}
			  """);

		var otherMembersIds = leagueMemberIds.GetRange(2, leagueMemberIds.Count - 2);
		foreach (var otherMemberId in otherMembersIds) {
			sb.AppendLine(
				$$"""
				  		title:{{otherMemberId}}.holder = {
				  			add_to_list = irtock3_confederation_members
				  		}
				  """);
		}

		sb.AppendLine("\t\tirtock3_confederation_name_setup_effect = yes");

		sb.AppendLine("\t\tscope:new_confederation = {");
		sb.AppendLine($"\t\t\tadd_confederation_member = title:{secondMemberId}.holder");
		foreach (var otherMemberId in otherMembersIds) {
			sb.AppendLine($"\t\t\tadd_confederation_member = title:{otherMemberId}.holder");
		}

		sb.AppendLine(
			"""
						irtock3_confederation_finish_setup_effect = yes
					}
					# Do some scope/list cleanup so there are no issues when setting up multiple confederations.
					clear_saved_scope = first_confed_member
					clear_saved_scope = second_confed_member
			""");
		foreach (var memberId in leagueMemberIds) {
			sb.AppendLine($"\t\ttitle:{memberId}.holder = {{ remove_from_list = irtock3_confederation_members }}");
		}
		sb.AppendLine();
	}
}