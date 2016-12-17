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
				args.Player.SendErrorMessage("语法无效! 正确语法: /reason(因) <原因>");
				args.Player.SendInfoMessage("你还剩 {0} 秒可以输入原因.", VotePlugin.Config.MaxAwaitingReasonTime - (int)(DateTime.UtcNow - data.StartedVote.Time).TotalSeconds);
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
				player.SendInfoMessage("{0} 发起了投票{1}. 原因: {2}.", data.StartedVote.Sponsor,
					TShock.Utils.ColorTag(data.StartedVote.ToString(), Color.SkyBlue),
					TShock.Utils.ColorTag(reason, Color.SkyBlue));
				player.SendSuccessMessage("输入 {0} 或 {1} 以参与投票. 否则 {2} 秒后, 系统视为自动弃权.",
					TShock.Utils.ColorTag("/赞成(assent)", Color.Cyan),
					TShock.Utils.ColorTag("/反对(dissent)", Color.Cyan),
					VotePlugin.Config.MaxAwaitingVotingTime);
			}

			TShock.Log.ConsoleInfo("{0} 发起了投票{1}. 原因: {2}.", args.Player.User.Name, data.StartedVote, reason);
		}

		internal void Vote(CommandArgs args) {
			if(!args.Player.HasPermission("vote.player.vote")) {
				args.Player.SendErrorMessage("你没有投票的权限!");
				return;
			}

			var assent = args.Message.ToLower().StartsWith("assent") || args.Message.ToLower().StartsWith("赞成");
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
					args.Player.SendErrorMessage("玩家名无效!");
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
					args.Player.SendErrorMessage("玩家名无效!");
					return;
				}
				if(data.OngoingVote != null && data.OngoingVote != targetData.StartedVote) {
					data.AwaitingVote = true;
					args.Player.SendErrorMessage("你必须先投票给 {0} ({1} 发起).",
						TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.LightCyan),
						TShock.Utils.ColorTag(data.OngoingVote.Sponsor, Color.LightCyan));
					return;
				}

				vote = targetData.StartedVote;
			}

			if(vote.Opponents.Intersect(vote.Proponents).Contains(args.Player.User.Name)) {
				args.Player.SendErrorMessage("你已经投过票了!");
				return;
			}

			args.Player.SendInfoMessage("真的要投票 {0} {1}?",
					TShock.Utils.ColorTag(assent ? "支持" : "反对", Color.OrangeRed),
					TShock.Utils.ColorTag(vote.ToString(), Color.Cyan));
			args.Player.SendInfoMessage("输入 {0} 以确定, {1} 以退出.", TShock.Utils.ColorTag("/y", Color.SkyBlue), TShock.Utils.ColorTag("/n", Color.SkyBlue));

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
				args.Player.SendSuccessMessage("你的选择已被取消; 使用 {0} 或 {1} 投票.",
					TShock.Utils.ColorTag("/赞成(assent)", Color.Cyan),
					TShock.Utils.ColorTag("/反对(dissent)", Color.Cyan));
				return;
			}

			if(data.Support)
				data.OngoingVote.Proponents.Add(args.Player.User.Name);
			else {
				data.OngoingVote.Opponents.Add(args.Player.User.Name);
			}

			if(!VotePlugin.Config.ShowResult)
				TSPlayer.All.SendInfoMessage("{0} 投票给了 {1}!",
				args.Player.User.Name,
				TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.SkyBlue));
			else
				TSPlayer.All.SendInfoMessage("{0} 投票{1}了 {2}!",
				args.Player.User.Name,
				TShock.Utils.ColorTag(data.Support ? "赞成" : "反对", Color.OrangeRed),
				TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.SkyBlue));
			TShock.Log.ConsoleInfo($"{args.Player.User.Name} 投票{(data.Support ? "赞成" : "反对")}了 {data.OngoingVote}!");

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

			player.SendErrorMessage("你没有在时限内给出你的原因, 投票已被取消.");
		}

		internal void OnVoteTimerElasped(Vote vote) {
			TShock.Players.Where(p => p != null).ForEach(p => {
				var data = p.GetData<PlayerData>(VotePlugin.VotePlayerData);

				if(_instance.Votes.Count == 1)
					data.AwaitingVote = data.AwaitingConfirm = false;
				// sponsor of vote
				if(data.StartedVote == vote) {
					p.SendMessage($"你发起的投票——{TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}——已经过期.", Color.Azure);
					data.AwaitingReason = false;
					data.StartedVote = null;
				}
				// those who /assent or /dissent but not /y
				if(data.OngoingVote == vote) {
					p.SendErrorMessage($"投票——{TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}——已结束. 你没有确认你的选择(/y).");
					data.AwaitingConfirm = false;
					data.OngoingVote = null;
				}
				// those who don't type /assent or /dissent
				if(vote.Opponents.All(n => p.User.Name != n) && vote.Proponents.All(n => p.User.Name != n)) {
					vote.Neutrals.Add(p.User.Name);
					p.SendMessage($"投票——{TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}——已弃权.", Color.Azure);
				}
			});
			if(vote.CheckPass())
				vote.Execute();
			TSPlayer.All.SendMessage(string.Format("{0} {1}通过. ({2} : {3} : {4})",
				TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue),
				vote.Succeed ? "成功" : "未",
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
				player.AwaitingResponse.Remove("赞成");
				player.AwaitingResponse.Remove("反对");
			} else {
				player.AddResponse("assent", obj => Vote((CommandArgs)obj));
				player.AddResponse("dissent", obj => Vote((CommandArgs)obj));
				player.AddResponse("赞成", obj => Vote((CommandArgs)obj));
				player.AddResponse("反对", obj => Vote((CommandArgs)obj));
			}
		}

		internal void AwaitReason(TSPlayer player, bool remove) {
			if(remove) {
				player.AwaitingResponse.Remove("reason");
				player.AwaitingResponse.Remove("因");
			} else {
				player.AddResponse("reason", obj => Reason((CommandArgs)obj));
				player.AddResponse("因", obj => Reason((CommandArgs)obj));
			}
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
			var cmdText = assent ? "/赞成" : "/反对";
			var text = new List<string> {
				"*** 当前存在多个投票, 你需要指明投票的发起者."
			};
			text.AddRange(_instance.Votes.Keys.Select(v => $"  * {v} ({v.Sponsor} 发起) -- {TShock.Utils.ColorTag($"{cmdText} {v.Sponsor}", Color.Cyan)}"));

			text.ForEach(player.SendErrorMessage);
		}
	}
}
