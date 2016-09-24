using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Timers;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using TShockAPI.Hooks;

namespace Vote {
	[ApiVersion(1, 24)]
	public class VotePlugin:TerrariaPlugin {
		public const string VotePlayerData = "votedata";
		internal static Configuration Config;
		internal Dictionary<Vote, Timer> Votes = new Dictionary<Vote, Timer>();
		internal static TSWheelPlayer Player;
		internal static Utils Utils;
		internal static VoteManager VotesHistory;

		public override string Name => "Vote";
		public override string Author => "MistZZT";
		public override string Description => "Gives players the right to vote in Terraria server running TShock.";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public VotePlugin(Main game) : base(game) { }

		public override void Initialize() {
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet, -100);

			GeneralHooks.ReloadEvent += OnReload;
		}

		protected override void Dispose(bool disposing) {
			if(disposing) {
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);

				GeneralHooks.ReloadEvent -= OnReload;
			}
			base.Dispose(disposing);
		}

		private void OnInitialize(EventArgs args) {
			Config = Configuration.Read(Configuration.FilePath);
			Config.Write(Configuration.FilePath);
			Config.LoadGroup();
			Player = new TSWheelPlayer();
			Utils = new Utils(this);
			VotesHistory = new VoteManager(TShock.DB);

			Commands.ChatCommands.Add(new Command("vote.player.startvote", StartVote, "vote", "投票", "v") {
				AllowServer = false,
				DoLog = false
			});
		}

		private void OnGreet(GreetPlayerEventArgs args) {
			var ply = TShock.Players[args.Who];
			if(ply == null)
				return;

			var data = new PlayerData(args.Who);
			ply.SetData(VotePlayerData, data);

			if(!ply.HasPermission("vote.player.vote"))
				return;

			if(Votes.Count == 0)
				return;

			foreach (var votePair in Votes.Where(v => v.Key.Proponents.All(name => !name.Equals(ply.User.Name)) && v.Key.Opponents.All(name => !name.Equals(ply.User.Name)))) {
				var vote = votePair.Key;

				data.AwaitingVote = true;
				ply.SendInfoMessage("投票——因{2}, 故{0}发起{1}——正在进行中. ({3}s后结束)", vote.Sponsor,
					TShock.Utils.ColorTag(vote.ToString(), Color.SkyBlue),
					TShock.Utils.ColorTag(vote.Reason, Color.SkyBlue),
					 Config.MaxAwaitingVotingTime - (int)(DateTime.UtcNow - vote.Time).TotalSeconds);
			}

			if(data.AwaitingVote)
				ply.SendSuccessMessage("输入 {0} 或 {1} 以参与投票",
					TShock.Utils.ColorTag("/赞成", Color.Cyan),
					TShock.Utils.ColorTag("/反对", Color.Cyan));
		}

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		private void StartVote(CommandArgs args) {
			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			var players = args.Parameters.Count < 2 ? null : TShock.Utils.FindPlayer(args.Parameters[1]);
			var data = args.Player.GetData<PlayerData>(VotePlayerData);
			if(data.StartedVote != null) {
				args.Player.SendErrorMessage("你上次发起的投票还未结束. ({0}s)",
					Config.MaxAwaitingVotingTime - (int)(DateTime.UtcNow - data.StartedVote.Time).TotalSeconds);
				return;
			}

			Vote vote;
			switch(cmd) {
				case "帮助":
				case "help":
					#region -- Help --
					int pageNumber;
					if(!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						return;

					var lines = new List<string>
					{
							"*** 如何发起投票:",
							"	** /vote(投票) <ban/kick/mute/kill> <玩家名>",
							"	** /reason(因) <原因>",
							" * 你必须在 {0} 秒内给出你的原因, 否则投票取消.".SFormat(Config.MaxAwaitingReasonTime),
							"*** 如何参与投票:",
							"	** {0} & {1}".SFormat(TShock.Utils.ColorTag("/赞成(assent)", Color.SkyBlue), TShock.Utils.ColorTag("/反对(dissent)", Color.SkyBlue)),
							"	** {0} & {1} 以确定投票.".SFormat(TShock.Utils.ColorTag("/y", Color.SkyBlue), TShock.Utils.ColorTag("/n", Color.SkyBlue)),
							" * /assent [发起者] 可以在多个投票中选择.",
							"*** 一些其他事项:",
							" * {0} 秒后投票结束, 未投票的玩家将视为弃权.".SFormat(Config.MaxAwaitingVotingTime),
							" * 如果你有任何建议, 在这里添加ISSUE: {0}.".SFormat("https://github.com/mistzzt/Vote/")
						};

					PaginationTools.SendPage(args.Player, pageNumber, lines,
						new PaginationTools.Settings {
							HeaderFormat = "投票说明 ({0}/{1}):",
							FooterFormat = "键入 {0}vote help {{0}} 以查看更多说明.".SFormat(Commands.Specifier)
						}
					);
					#endregion
					return;
				case "封禁":
				case "ban":
					#region -- Ban --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("语法无效! 正确语法: /v ban <玩家名>");
						return;
					}
					if(players.Count == 0) {
						var user = TShock.Users.GetUserByName(args.Parameters[1]);
						if(user != null) {
							if(TShock.Groups.GetGroupByName(user.Group).HasPermission(Permissions.immunetoban)) {
								args.Player.SendErrorMessage("你无法封禁 {0}!", user.Name);
								return;
							}
						}
						args.Player.SendErrorMessage("玩家名或账户名无效!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Ban);
					break;
				case "驱逐":
				case "kick":
					#region -- Kick --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("语法无效! 正确语法: /v kick <玩家名>");
						return;
					}
					if(players.Count == 0) {
						args.Player.SendErrorMessage("玩家名无效!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Kick);
					break;
				case "禁言":
				case "mute":
					#region -- Mute --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("语法无效! 正确语法: /v mute <玩家名>");
						return;
					}
					if(players.Count == 0) {
						args.Player.SendErrorMessage("玩家名无效!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Mute);
					break;
				case "杀":
				case "kill":
					#region -- Kill --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("语法无效! 正确语法: /v kill <玩家名>");
						return;
					}
					if(players.Count == 0) {
						args.Player.SendErrorMessage("玩家名无效!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Kill);
					break;
				default:
					if(cmd.StartsWith(Commands.Specifier)) {
						#region -- Command --
						var commandText = string.Join(" ", args.Parameters).Remove(0, 1).TrimStart();
						var commandName = commandText.Contains(" ")
										&& commandText[commandText.IndexOf(" ", StringComparison.Ordinal) - 1] != '\\'
							? commandText.Substring(0, commandText.IndexOf(" ", StringComparison.Ordinal))
							: commandText;

						IEnumerable<Command> cmds = Commands.ChatCommands.FindAll(c => c.HasAlias(commandName));
						if(!cmds.Any()) {
							args.Player.SendErrorMessage("未找到此指令.");
							return;
						}
						if(cmds.Count() > 1) {
							args.Player.SendErrorMessage("存在相同名称的多个指令!");
							return;
						}

						var command = cmds.ElementAt(0);
						if(!command.Permissions.Any(Config.ExecutiveGroup.HasPermission)) {
							args.Player.SendErrorMessage("缺少执行该指令的权限, 无法投票执行.");
							return;
						}
						#endregion
						vote = new Vote(args.Player.User.Name, $"{Commands.Specifier}{commandText}", VoteType.Command);
						break;
					}
					args.Player.SendErrorMessage("语法无效! 输入 /vote help 获取帮助.");
					return;
			}

			var timer = new Timer(Config.MaxAwaitingReasonTime * 1000) { AutoReset = false, Enabled = true };
			timer.Elapsed += (s, e) => Utils.OnReasonTimerElasped(vote, args.Player);

			data.StartedVote = vote;
			data.AwaitingReason = true;
			Votes.Add(vote, timer);

			args.Player.SendSuccessMessage("使用 {0} 以继续发起投票.", TShock.Utils.ColorTag("/因 <原因>", Color.SkyBlue));
		}

		private void OnReload(ReloadEventArgs args) {
			Config = Configuration.Read(Configuration.FilePath);
			Config.Write(Configuration.FilePath);
			Config.LoadGroup();
			Player = new TSWheelPlayer();
		}
	}
}

