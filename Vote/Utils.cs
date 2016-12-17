using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Microsoft.Xna.Framework;
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
				args.Player.SendInfoMessage("You still have {0} seconds to enter your reason using {1}.", VotePlugin.Config.MaxAwaitingReasonTime - (int)(DateTime.UtcNow - data.StartedVote.Time).TotalSeconds, TShock.Utils.ColorTag("/reason <Reason>", Color.Cyan));
				return;
			}

			var reason = string.Join(" ", args.Parameters);
			var timer = _instance.Votes[data.StartedVote];
			data.StartedVote.Reason = reason;
			data.AwaitingReason = false;
			timer.Enabled = false;
			timer = _instance.Votes[data.StartedVote] = new Timer(VotePlugin.Config.MaxAwaitingVotingTime * 1000) { AutoReset = false, Enabled = true };
			timer.Elapsed += (s, e) => OnVoteTimerElasped(data.StartedVote);

			foreach(var player in TShock.Players.Where(p => p != null && p.IsLoggedIn && p.HasPermission("vote.player.vote"))) {
				player.GetData<PlayerData>(VotePlugin.VotePlayerData).AwaitingVote = true;
				player.SendInfoMessage("Vote: {1} for {2} by {0} started.", data.StartedVote.Sponsor,
					TShock.Utils.ColorTag(data.StartedVote.ToString(), Color.SkyBlue),
					TShock.Utils.ColorTag(reason, Color.SkyBlue));
				player.SendSuccessMessage("Use {0} or {1} to cast a vote in {2} seconds. Otherwise, you will abstain from voting.",
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
					data.AwaitingVote = true;
					PrintMultipleVoteError(args.Player, assent);
					return;
				}
				vote = _instance.Votes.ElementAt(0).Key;
			} else {
				var players = TShock.Utils.FindPlayer(args.Parameters[0]);
				if(players.Count == 0) {
					data.AwaitingVote = true;
					args.Player.SendErrorMessage("Invalid player!");
					return;
				}
				if(players.Count > 1) {
					data.AwaitingVote = true;
					TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
					return;
				}
				var targetData = players[0].GetData<PlayerData>(VotePlugin.VotePlayerData);
				if(targetData.StartedVote == null) {
					data.AwaitingVote = true;
					args.Player.SendErrorMessage("Invalid player!");
					return;
				}
				if(data.OngoingVote != null && data.OngoingVote != targetData.StartedVote) {
					data.AwaitingVote = true;
					args.Player.SendErrorMessage("You must first finish {0} (by {1}).",
						TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.LightCyan),
						TShock.Utils.ColorTag(data.OngoingVote.Sponsor, Color.LightCyan));
					return;
				}

				vote = targetData.StartedVote;
			}

			if(vote.Opponents.Intersect(vote.Proponents).Contains(args.Player.User.Name)) {
				args.Player.SendErrorMessage("You have voted on this issue!");
				return;
			}

			args.Player.SendInfoMessage("Really vote {0} {1}?",
					TShock.Utils.ColorTag(assent ? "for" : "against", Color.OrangeRed),
					TShock.Utils.ColorTag(vote.ToString(), Color.Cyan));
			args.Player.SendInfoMessage("Use {0} to confirm, {1} to cancel.", TShock.Utils.ColorTag("/y", Color.SkyBlue), TShock.Utils.ColorTag("/n", Color.SkyBlue));

			data.OngoingVote = vote;
			data.Support = assent;
			data.AwaitingConfirm = true;
			data.AwaitingVote = false;
			args.Player.AddResponse("y", obj => Confirm((CommandArgs)obj));
		}

		internal void Confirm(CommandArgs args) {
			var data = args.Player.GetData<PlayerData>(VotePlugin.VotePlayerData);

			if(args.Message.ToLower().StartsWith("n")) {
				data.AwaitingConfirm = false;
				data.AwaitingVote = true;
				data.OngoingVote = null;
				args.Player.SendSuccessMessage("Your answer was canceled. Use {0} or {1} to cast a vote.",
					TShock.Utils.ColorTag("/assent", Color.Cyan),
					TShock.Utils.ColorTag("/dissent", Color.Cyan));
				return;
			}

			if(data.Support)
				data.OngoingVote.Proponents.Add(args.Player.User.Name);
			else {
				data.OngoingVote.Opponents.Add(args.Player.User.Name);
			}

			if(!VotePlugin.Config.ShowResult)
				TSPlayer.All.SendInfoMessage("{0} voted on {1}!",
				args.Player.User.Name,
				TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.SkyBlue));
			else
				TSPlayer.All.SendInfoMessage("{0} voted {1} {2}!",
				args.Player.User.Name,
				TShock.Utils.ColorTag(data.Support ? "for" : "against", Color.OrangeRed),
				TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.SkyBlue));
			TShock.Log.ConsoleInfo($"{args.Player.User.Name} voted {(data.Support ? "for" : "against")} {data.OngoingVote}!");

			data.OngoingVote = null;
			data.Support = false;
			data.AwaitingConfirm = false;
		}

		internal void OnReasonTimerElasped(Vote vote, TSPlayer player) {
			_instance.Votes.Remove(vote);

			if (player == null || !player.Active || vote.Sponsor != player.Name)
				return;

			var data = player.GetData<PlayerData>(VotePlugin.VotePlayerData);

			data.AwaitingReason = false;
			
			data.StartedVote = null;

			player.SendErrorMessage("You haven't given your reason, so your vote is canceled.");
		}

		internal void OnVoteTimerElasped(Vote vote) {
			TShock.Players.Where(p => p != null).ForEach(p => {
				var data = p.GetData<PlayerData>(VotePlugin.VotePlayerData);

				if(_instance.Votes.Count == 1)
					data.AwaitingVote = data.AwaitingConfirm = false;
				// sponsor of vote
				if(data.StartedVote == vote) {
					p.SendMessage($"Your vote {TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)} has expired.", Color.Azure);
					data.AwaitingReason = false;
					data.StartedVote = null;
				}
				// those who /assent or /dissent but not /y
				if(data.OngoingVote == vote) {
					p.SendErrorMessage($"The vote {TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)} has expired. You haven't confirmed.");
					data.AwaitingConfirm = false;
					data.OngoingVote = null;
				}
				// those who don't type /assent or /dissent
				if(vote.Opponents.All(n => p.User.Name != n) && vote.Proponents.All(n => p.User.Name != n)) {
					vote.Neutrals.Add(p.User.Name);
					p.SendMessage($"You have abstained from voting on {TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}.", Color.Azure);
				}
			});
			if(vote.CheckPass())
				vote.Execute();
			TSPlayer.All.SendMessage(string.Format("{0} has {1}passed. ({2} : {3} : {4})",
				TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue),
				vote.Succeed ? "" : "not ",
				TShock.Utils.ColorTag(vote.Proponents.Count.ToString(), Color.GreenYellow),
				vote.Neutrals.Count,
				TShock.Utils.ColorTag(vote.Opponents.Count.ToString(), Color.OrangeRed)), Color.White);
			TShock.Log.ConsoleInfo("{0} has {1}passed. ({2} : {3} : {4})", vote.ToString(), vote.Succeed ? "" : "not ", vote.Proponents.Count.ToString(), vote.Neutrals.Count.ToString(), vote.Opponents.Count.ToString());
			VotePlugin.VotesHistory.AddVote(vote);
			// remove references and responses
			_instance.Votes.Remove(vote);
			
		}

		internal void AwaitVote(TSPlayer player, bool remove) {
			if(remove) {
				player.AwaitingResponse.Remove("assent");
				player.AwaitingResponse.Remove("dissent");
			} else {
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
			if(remove) {
				player.AwaitingResponse.Remove("y");
				player.AwaitingResponse.Remove("n");
			} else {
				player.AddResponse("y", obj => Confirm((CommandArgs)obj));
				player.AddResponse("n", obj => Confirm((CommandArgs)obj));
			}
		}

		internal void PrintMultipleVoteError(TSPlayer player, bool assent) {
			var count = _instance.Votes.Count;
			var cmdText = assent ? "/assent" : "/dissent";
			var text = new List<string> {
				$"*** As currently there are {count} vote{(count > 1 ? "s":"")}, you have to determine which one to choose."
			};
			text.AddRange(_instance.Votes.Keys.Select(v => $"  * {v} (by {v.Sponsor}) -- {TShock.Utils.ColorTag($"{cmdText} {v.Sponsor}", Color.Cyan)}"));

			text.ForEach(player.SendErrorMessage);
		}
	}
}
