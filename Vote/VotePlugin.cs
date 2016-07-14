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
	[ApiVersion(1, 23)]
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

			Commands.ChatCommands.Add(new Command("vote.player.startvote", StartVote, "vote", "v") {
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
				ply.SendInfoMessage("Vote: {1} for {2} by {0} is in progress. ({3}s last)", vote.Sponsor,
					TShock.Utils.ColorTag(vote.ToString(), Color.SkyBlue),
					TShock.Utils.ColorTag(vote.Reason, Color.SkyBlue),
					 Config.MaxAwaitingVotingTime - (int)(DateTime.UtcNow - vote.Time).TotalSeconds);
			}

			if(data.AwaitingVote)
				ply.SendSuccessMessage("Use {0} or {1} to cast a vote.",
					TShock.Utils.ColorTag("/assent", Color.Cyan),
					TShock.Utils.ColorTag("/dissent", Color.Cyan));
		}

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		private void StartVote(CommandArgs args) {
			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			var players = args.Parameters.Count < 2 ? null : TShock.Utils.FindPlayer(args.Parameters[1]);
			var data = args.Player.GetData<PlayerData>(VotePlayerData);
			if(data.StartedVote != null) {
				args.Player.SendErrorMessage("You can't initate a new vote until the former one you started ended. ({0}s)",
					Config.MaxAwaitingVotingTime - (int)(DateTime.UtcNow - data.StartedVote.Time).TotalSeconds);
				return;
			}

			Vote vote;
			switch(cmd) {
				case "help":
					#region -- Help --
					int pageNumber;
					if(!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						return;

					var lines = new List<string>
					{
							"*** How to sponsor a vote:",
							"	** /vote <ban/kick/mute/kill> <player>",
							"	** /reason <Reason>",
							" * You must give your reason in {0} seconds.".SFormat(Config.MaxAwaitingReasonTime),
							"*** How to take a vote on an issue:",
							"	** {0} & {1}".SFormat(TShock.Utils.ColorTag("/assent", Color.SkyBlue), TShock.Utils.ColorTag("/dissent", Color.SkyBlue)),
							"	** {0} & {1} to confirm/cancel your answer.".SFormat(TShock.Utils.ColorTag("/y", Color.SkyBlue), TShock.Utils.ColorTag("/n", Color.SkyBlue)),
							" * Use /assent [player] when dealing with more than one votes.",
							"*** Other things:",
							" * After {0} seconds, a vote will get its end.".SFormat(Config.MaxAwaitingVotingTime),
							" * If you have any advice, create an issue here {0}.".SFormat("https://github.com/mistzzt/Vote/")
						};

					PaginationTools.SendPage(args.Player, pageNumber, lines,
						new PaginationTools.Settings {
							HeaderFormat = "Vote instructions ({0}/{1}):",
							FooterFormat = "Type {0}vote help {{0}} for more instructions.".SFormat(Commands.Specifier)
						}
					);
					#endregion
					return;
				case "ban":
					#region -- Ban --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /v ban <player>");
						return;
					}
					if(players.Count == 0) {
						var user = TShock.Users.GetUserByName(args.Parameters[1]);
						if(user != null) {
							if(TShock.Groups.GetGroupByName(user.Group).HasPermission(Permissions.immunetoban)) {
								args.Player.SendErrorMessage("You can't ban {0}!", user.Name);
								return;
							}
						}
						args.Player.SendErrorMessage("Invalid player or account!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Ban);
					break;
				case "kick":
					#region -- Kick --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /v kick <player>");
						return;
					}
					if(players.Count == 0) {
						args.Player.SendErrorMessage("Invalid player!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Kick);
					break;
				case "mute":
					#region -- Mute --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /v mute <player>");
						return;
					}
					if(players.Count == 0) {
						args.Player.SendErrorMessage("Invalid player!");
						return;
					}
					if(players.Count > 1) {
						TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
						return;
					}
					#endregion
					vote = new Vote(args.Player.User.Name, args.Parameters[1], VoteType.Mute);
					break;
				case "kill":
					#region -- Kill --
					if(args.Parameters.Count < 2) {
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /v kill <player>");
						return;
					}
					if(players.Count == 0) {
						args.Player.SendErrorMessage("Invalid player!");
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
							args.Player.SendErrorMessage("Invalid command entered.");
							return;
						}
						if(cmds.Count() > 1) {
							args.Player.SendErrorMessage("More than one command matched!");
							return;
						}

						var command = cmds.ElementAt(0);
						if(!command.Permissions.Any(Config.ExecutiveGroup.HasPermission)) {
							args.Player.SendErrorMessage("{0} can't be executed due to lack of permission.");
							return;
						}
						#endregion
						vote = new Vote(args.Player.User.Name, $"{Commands.Specifier}{commandText}", VoteType.Command);
						break;
					}
					args.Player.SendErrorMessage("Invalid syntax! Type /vote help for a list of instructions.");
					return;
			}

			var timer = new Timer(Config.MaxAwaitingReasonTime * 1000) { AutoReset = false, Enabled = true };
			timer.Elapsed += (s, e) => Utils.OnReasonTimerElasped(args.Player);

			data.StartedVote = vote;
			data.AwaitingReason = true;
			Votes.Add(vote, timer);

			args.Player.SendSuccessMessage("Vote will be started after using {0}.", TShock.Utils.ColorTag("/reason <Reason>", Color.SkyBlue));
		}

		private void OnReload(ReloadEventArgs args) {
			Config = Configuration.Read(Configuration.FilePath);
			Config.Write(Configuration.FilePath);
			Config.LoadGroup();
			Player = new TSWheelPlayer();
		}
	}
}

