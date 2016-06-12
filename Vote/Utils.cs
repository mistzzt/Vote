using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TShockAPI;

namespace Vote {
	internal class Utils {
		private readonly VotePlugin _instance;

		public Utils(VotePlugin instance) {
			_instance = instance;
		}

		internal void Reason(CommandArgs args) {
			var data = args.Player.GetData<PlayerData>(VotePlugin.VotePlayerData);
			if(args.Parameters.Count == 0) {
				data.AwaitingReason = true;
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /reason <Reason>");
				args.Player.SendInfoMessage("You still have {0} seconds to enter your reason using {1}.", VotePlugin.Config.MaxAwaitingReasonTime - (DateTime.UtcNow - data.StartedVote.Time).Seconds, TShock.Utils.ColorTag("/reason <Reason>", Color.Cyan));
				return;
			}

			var reason = string.Join(" ", args.Parameters);
			var timer = _instance.Votes[data.StartedVote];
			data.StartedVote.Reason = reason;
			timer.Enabled = false;
			timer = _instance.Votes[data.StartedVote] = new Timer(VotePlugin.Config.MaxAwaitingVotingTime * 1000) { AutoReset = false, Enabled = true };
			timer.Elapsed += (s, e) => OnVoteTimerElasped(data.StartedVote);

			foreach(var player in TShock.Players.Where(p => p != null && p.IsLoggedIn && p.HasPermission("vote.player.vote"))) {
				data.AwaitingVote = true;
				player.SendInfoMessage("{0} started a new vote {1} for {2}.", data.StartedVote.Sponsor, data.StartedVote, reason);
				player.SendInfoMessage("Use {0} or {1} to cast a vote in {2} minutes. Otherwise, you will abstain from voting.",
					TShock.Utils.ColorTag("/assent", Color.Cyan),
					TShock.Utils.ColorTag("/dissent", Color.Cyan),
					VotePlugin.Config.MaxAwaitingVotingTime);
			}

			TShock.Log.ConsoleInfo("{0} started a new vote {1} for {2}.", args.Player.User.Name, data.StartedVote, reason);
		}

		internal void Vote(CommandArgs args) {
			if(!args.Player.HasPermission("vote.player.vote")) {
				args.Player.SendErrorMessage("You don't have permission to vote!");
				return;
			}

			var assent = args.Message.ToLower().StartsWith("assent");
			var data = args.Player.GetData<PlayerData>(VotePlugin.VotePlayerData);
			Vote vote;

			if(args.Parameters.Count == 0) {
				if(_instance.Votes.Count > 1) {
					PrintMultipleVoteError(args.Player, assent);
					return;
				}
				vote = _instance.Votes.ElementAt(0).Key;
			} else {
				var players = TShock.Utils.FindPlayer(args.Parameters[0]);
				if(players.Count == 0) {
					args.Player.SendErrorMessage("Invalid player!");
					return;
				}
				if(players.Count > 1) {
					TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
					return;
				}
				var targetData = args.Player.GetData<PlayerData>(VotePlugin.VotePlayerData);
				if(targetData.StartedVote == null) {
					args.Player.SendErrorMessage("Invalid player!");
					return;
				}
				if(data.OngoingVote != targetData.StartedVote) {
					args.Player.SendErrorMessage("You must first finish {0} (by {1}).", data.OngoingVote, data.OngoingVote.Sponsor);
					return;
				}

				vote = targetData.StartedVote;
			}

			args.Player.SendInfoMessage("Do you really decide to vote {0} {1}?",
					assent ? "for" : "against",
					TShock.Utils.ColorTag(vote.ToString(), Color.Cyan));
			args.Player.SendInfoMessage("Use {0} to confirm your answer.", TShock.Utils.ColorTag("/y", Color.Cyan));

			data.OngoingVote = vote;
			data.Support = assent;
			data.AwaitingConfirm = true;
			data.AwaitingVote = false;
			args.Player.AddResponse("y", obj => Confirm((CommandArgs)obj));
		}

		internal void Confirm(CommandArgs args) {
			var data = args.Player.GetData<PlayerData>(VotePlugin.VotePlayerData);

			if(data.Support)
				data.OngoingVote.Proponents.Add(args.Player.User.Name);
			else {
				data.OngoingVote.Opponents.Add(args.Player.User.Name);
			}

			TSPlayer.All.SendInfoMessage($"{args.Player.User.Name} voted on {data.OngoingVote}!");
			TShock.Log.ConsoleInfo($"{args.Player.User.Name} voted {(data.Support ? "for" : "against")} {data.OngoingVote}!");

			data.OngoingVote = null;
			data.Support = false;
			data.AwaitingConfirm = false;
		}

		internal void OnReasonTimerElasped(TSPlayer player) {
			var data = player.GetData<PlayerData>(VotePlugin.VotePlayerData);
			if(!data.AwaitingReason)
				return;

			player.AwaitingResponse.Remove("reason");
			_instance.Votes.Remove(data.StartedVote);
			data.StartedVote = null;

			player.SendErrorMessage("You haven't given your reason, so your vote is canceled.");
		}

		internal void OnConfirmTimerElasped(TSPlayer player, Vote vote) {
			//ongoing
		}

		internal void OnVoteTimerElasped(Vote vote) {
			vote.Execute();
			// remove references and responses
			_instance.Votes.Remove(vote);
			TShock.Players.Where(p => p != null).ForEach(p => {
				var data = p.GetData<PlayerData>(VotePlugin.VotePlayerData);
				if(data.StartedVote == vote || data.OngoingVote == vote) {
					data.AwaitingVote = false;
					data.AwaitingConfirm = false;
				}
			});
		}

		internal void AwaitVote(TSPlayer player, bool remove) {
			if(remove) {
				player.AwaitingResponse.Remove("assent");
				player.AwaitingResponse.Remove("dissent");
			}
			else {
				player.AddResponse("assent", obj => Vote((CommandArgs)obj));
				player.AddResponse("dissent", obj => Vote((CommandArgs)obj));
			}		
		}

		internal void AwaitReason(TSPlayer player, bool remove) {
			if(remove)
				player.AwaitingResponse.Remove("reason");
			else
				player.AddResponse("reason", obj => Reason((CommandArgs)obj));
		}

		internal void AwaitConfirm(TSPlayer player, bool remove) {
			if(remove)
				player.AwaitingResponse.Remove("y");
			else
				player.AddResponse("y", obj => Confirm((CommandArgs)obj));
		}

		internal void PrintMultipleVoteError(TSPlayer player, bool assent) {
			var count = _instance.Votes.Count;
			var cmdText = assent ? "/assent" : "dissent";
			var text = new List<string> {
				$"*** As currently there are {count} vote{(count > 1 ? "s":"")}, you have to determine which one to choose."
			};
			text.AddRange(_instance.Votes.Keys.Select(v => $"  * {v} (by {v.Sponsor}) -- {TShock.Utils.ColorTag($"{cmdText} {v.Sponsor}", Color.Cyan)}"));

			text.ForEach(player.SendErrorMessage);
		}
	}
}
