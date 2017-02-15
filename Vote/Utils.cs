using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TShockAPI;

namespace Vote
{
	internal static class Utils
	{
		internal static void Reason(CommandArgs args)
		{
			var data = PlayerData.GetData(args.Player);

			if (args.Parameters.Count == 0)
			{
				data.AwaitingReason = true;
				args.Player.SendErrorMessage("语法无效! 正确语法: /reason(因) <原因>");
				args.Player.SendInfoMessage("你还剩 {0} 秒可以输入原因.",
					VotePlugin.Config.MaxAwaitingReasonTime - (int)(DateTime.UtcNow - data.StartedVote.Time).TotalSeconds);
				return;
			}

			data.AwaitingReason = false;

			var vote = data.StartedVote;
			vote.Reason = string.Join(" ", args.Parameters);
			vote.ResetTimer();

			foreach (var player in TShock.Players.Where(p => p?.IsLoggedIn == true && p.HasPermission("vote.player.vote")))
			{
				PlayerData.GetData(player).AwaitingVote = true;
				player.SendInfoMessage("{0} 发起了投票{1}. 原因: {2}.", vote.Sponsor,
					TShock.Utils.ColorTag(vote.ToString(), Color.SkyBlue),
					TShock.Utils.ColorTag(vote.Reason, Color.SkyBlue));
				player.SendSuccessMessage("输入 {0} 或 {1} 以参与投票. 否则 {2} 秒后, 系统视为自动弃权.",
					TShock.Utils.ColorTag("/赞成(assent)", Color.Cyan),
					TShock.Utils.ColorTag("/反对(dissent)", Color.Cyan),
					VotePlugin.Config.MaxAwaitingVotingTime);
			}

			TShock.Log.ConsoleInfo("{0}因{2}发起了投票{1}", args.Player.Name, vote, vote.Reason);
		}

		internal static void Vote(CommandArgs args)
		{
			if (!args.Player.HasPermission("vote.player.vote"))
			{
				args.Player.SendErrorMessage("你没有投票的权限!");
				return;
			}

			var assent = args.Message.ToLower().StartsWith("assent") || args.Message.ToLower().StartsWith("赞成");
			var data = PlayerData.GetData(args.Player);
			Vote vote;

			if (args.Parameters.Count == 0)
			{
				if (VotePlugin.Votes.Count > 1)
				{
					data.AwaitingVote = true;
					PrintMultipleVoteError(args.Player, assent);
					return;
				}
				vote = VotePlugin.Votes.Single();
			}
			else
			{
				var spNameLower = args.Parameters[0].ToLowerInvariant();
				var list = new List<Vote>();
				foreach (var v in VotePlugin.Votes)
				{
					var sp = v.Sponsor.ToLowerInvariant();
					if (sp.Equals(spNameLower, StringComparison.Ordinal))
					{
						list.Add(v);
						break;
					}
					if (sp.StartsWith(spNameLower))
					{
						list.Add(v);
					}
				}

				if (list.Count == 1)
				{
					vote = list.Single();
				}
				else if (list.Count > 1)
				{
					data.AwaitingVote = true;
					TShock.Utils.SendMultipleMatchError(args.Player, list.Select(l => l.Sponsor));
					return;
				}
				else
				{
					data.AwaitingVote = true;
					args.Player.SendErrorMessage("玩家名无效!");
					return;
				}

				if (data.OngoingVote != null && data.OngoingVote != vote)
				{
					data.AwaitingVote = true;
					args.Player.SendErrorMessage("你必须先投票给 {0} ({1} 发起).",
						TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.LightCyan),
						TShock.Utils.ColorTag(data.OngoingVote.Sponsor, Color.LightCyan));
					return;
				}
			}

			if (vote.Opponents.Union(vote.Proponents).Contains(args.Player.User.Name))
			{
				data.AwaitingVote = true;
				args.Player.SendErrorMessage("你已经投过票了!");
				return;
			}

			args.Player.SendInfoMessage("真的要投票{0}{1}?",
					TShock.Utils.ColorTag(assent ? "支持" : "反对", Color.OrangeRed),
					TShock.Utils.ColorTag(vote.ToString(), Color.Cyan));
			args.Player.SendInfoMessage("输入 {0} 以确定, {1} 以退出.",
					TShock.Utils.ColorTag("/y", Color.SkyBlue),
					TShock.Utils.ColorTag("/n", Color.SkyBlue));

			data.OngoingVote = vote;
			data.Support = assent;
			data.AwaitingConfirm = true;
			data.AwaitingVote = false;
		}

		internal static void Confirm(CommandArgs args)
		{
			var data = PlayerData.GetData(args.Player);

			if (args.Message.ToLower().StartsWith("n"))
			{
				data.AwaitingConfirm = false;
				data.AwaitingVote = true;
				data.OngoingVote = null;
				args.Player.SendSuccessMessage("你的选择已被取消; 使用 {0} 或 {1} 投票.",
					TShock.Utils.ColorTag("/赞成(assent)", Color.Cyan),
					TShock.Utils.ColorTag("/反对(dissent)", Color.Cyan));
				return;
			}

			if (data.Support)
			{
				data.OngoingVote.Proponents.Add(args.Player.User.Name);
			}
			else
			{
				data.OngoingVote.Opponents.Add(args.Player.User.Name);
			}

			if (!VotePlugin.Config.ShowResult)
				TSPlayer.All.SendInfoMessage("{0}在{1}的投票里表决了!",
					args.Player.User.Name,
					TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.SkyBlue));
			else
				TSPlayer.All.SendInfoMessage("{0}投票{1}了{2}!",
					args.Player.User.Name,
					TShock.Utils.ColorTag(data.Support ? "赞成" : "反对", Color.OrangeRed),
					TShock.Utils.ColorTag(data.OngoingVote.ToString(), Color.SkyBlue));
			TShock.Log.ConsoleInfo($"{args.Player.User.Name}投票{(data.Support ? "赞成" : "反对")}了{data.OngoingVote}!");

			data.OngoingVote = null;
			data.Support = false;
			data.AwaitingConfirm = false;
			data.AwaitingVote = VotePlugin.Votes.Count > 1;
		}

		internal static void OnReasonTimerElasped(Vote vote, TSPlayer player)
		{
			VotePlugin.Votes.Remove(vote);

			if (player?.Active != true || !vote.Sponsor.Equals(player.Name, StringComparison.Ordinal))
				return;

			var data = PlayerData.GetData(player);

			data.AwaitingReason = false;

			data.StartedVote = null;

			player.SendErrorMessage("你没有在时限内给出投票原因, 投票已被取消.");
		}

		internal static void OnVoteTimerElasped(Vote vote)
		{
			foreach (var player in TShock.Players.Where(p => p?.Active == true))
			{
				var data = PlayerData.GetData(player);

				if (VotePlugin.Votes.Count == 1)
					data.AwaitingVote = data.AwaitingConfirm = false; // without other votes
				// sponsor of vote
				if (data.StartedVote == vote)
				{
					player.SendMessage($"你发起的投票——{TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}——已经过期。", Color.Azure);
					data.AwaitingReason = false;
					data.StartedVote = null;
				}
				// those who /assent or /dissent but not /y
				else if (data.OngoingVote == vote)
				{
					player.SendErrorMessage($"投票——{TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}——已结束。");
					data.AwaitingConfirm = false;
					data.OngoingVote = null;
				}
				// those who don't type /assent or /dissent
				else if (vote.Opponents.Union(vote.Proponents).All(ply => !player.User.Name.Equals(ply, StringComparison.Ordinal)))
				{
					vote.Neutrals.Add(player.User.Name);
					player.SendMessage($"投票——{TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue)}——已弃权。", Color.Azure);
				}
			}

			vote.CheckPass();
			vote.Execute();

			TSPlayer.All.SendMessage(string.Format("{0}{1}通过。 ({2} : {3} : {4})",
														TShock.Utils.ColorTag(vote.ToString(), Color.DeepSkyBlue),
														vote.Succeed ? "成功" : "未",
														TShock.Utils.ColorTag(vote.Proponents.Count.ToString(), Color.GreenYellow),
														vote.Neutrals.Count,
														TShock.Utils.ColorTag(vote.Opponents.Count.ToString(), Color.OrangeRed)),
									Color.White);

			TShock.Log.ConsoleInfo("{0}{1}通过。 ({2} : {3} : {4})",
				vote.ToString(),
				vote.Succeed ? "成功" : "未",
				vote.Proponents.Count.ToString(),
				vote.Neutrals.Count.ToString(),
				vote.Opponents.Count.ToString());

			VotePlugin.VotesHistory.AddVote(vote);

			VotePlugin.Votes.Remove(vote);
		}

		internal static void AwaitVote(TSPlayer player, bool remove)
		{
			if (remove)
			{
				player.AwaitingResponse.Remove("assent");
				player.AwaitingResponse.Remove("dissent");
				player.AwaitingResponse.Remove("赞成");
				player.AwaitingResponse.Remove("反对");
			}
			else
			{
				player.AddResponse("assent", obj => Vote((CommandArgs)obj));
				player.AddResponse("dissent", obj => Vote((CommandArgs)obj));
				player.AddResponse("赞成", obj => Vote((CommandArgs)obj));
				player.AddResponse("反对", obj => Vote((CommandArgs)obj));
			}
		}

		internal static void AwaitReason(TSPlayer player, bool remove)
		{
			if (remove)
			{
				player.AwaitingResponse.Remove("reason");
				player.AwaitingResponse.Remove("因");
			}
			else
			{
				player.AddResponse("reason", obj => Reason((CommandArgs)obj));
				player.AddResponse("因", obj => Reason((CommandArgs)obj));
			}
		}

		internal static void AwaitConfirm(TSPlayer player, bool remove)
		{
			if (remove)
			{
				player.AwaitingResponse.Remove("y");
				player.AwaitingResponse.Remove("n");
			}
			else
			{
				player.AddResponse("y", obj => Confirm((CommandArgs)obj));
				player.AddResponse("n", obj => Confirm((CommandArgs)obj));
			}
		}

		internal static void PrintMultipleVoteError(TSPlayer player, bool assent)
		{
			var count = VotePlugin.Votes.Count;
			var cmdText = assent ? "/赞成" : "/反对";
			var text = new List<string> {
				$"*** 当前存在多个投票，你需要选择投票。"
			};
			text.AddRange(VotePlugin.Votes.Select(v => $"  * {v} ({v.Sponsor}发起) -- {TShock.Utils.ColorTag($"{cmdText} {v.Sponsor}", Color.Cyan)}"));

			text.ForEach(player.SendErrorMessage);
		}
	}
}
