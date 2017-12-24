using System;
using System.Collections.Generic;
using System.Timers;
using TShockAPI;

namespace Vote
{
	public class Vote
	{
		public DateTime Time { get; set; }

		public string Sponsor { get; set; }

		public string Target { get; set; }

		public VoteType Type { get; set; }

		public string Reason { get; set; }

		public bool Succeed { get; private set; }

		public bool Executed { get; private set; }

		public readonly List<string> Proponents = new List<string>();

		public readonly List<string> Opponents = new List<string>();

		public readonly List<string> Neutrals = new List<string>();

		private Timer _voteTimer;

		public Vote(TSPlayer player, string target, VoteType type)
		{
			Sponsor = player.Account?.Name ?? player.Name;
			Target = target;
			Type = type;
			Time = DateTime.UtcNow;

			_voteTimer = new Timer(VotePlugin.Config.MaxAwaitingReasonTime * 1000) { AutoReset = false, Enabled = true };
			_voteTimer.Elapsed += (s, e) => Utils.OnReasonTimerElasped(this, player);
		}

		public Vote(TSPlayer player, string target, DateTime time, VoteType type) : this(player, target, type)
		{
			Time = time;
		}

		public void ResetTimer()
		{
			_voteTimer.Dispose();
			_voteTimer = new Timer
			{
				AutoReset = false,
				Enabled = true,
				Interval = VotePlugin.Config.MaxAwaitingVotingTime * 1000
			};
			_voteTimer.Elapsed += (s, e) => Utils.OnVoteTimerElasped(this);
		}

		public void CheckPass()
		{
			Succeed = Proponents.Count > Opponents.Count;
		}

		public void Execute()
		{
			if (!Succeed)
				return;

			switch (Type)
			{
				case VoteType.Ban:
					Commands.HandleCommand(VotePlugin.Player, $"/ban add \"{Target}\" \"{Reason}\"");
					break;
				case VoteType.Kick:
				case VoteType.Mute:
					Commands.HandleCommand(VotePlugin.Player, $"/{Type} \"{Target}\" \"{Reason}\"");
					break;
				case VoteType.Kill:
					Commands.HandleCommand(VotePlugin.Player, $"/{Type} {Target}");
					break;
				case VoteType.Command:
					Commands.HandleCommand(VotePlugin.Player, Target);
					break;
			}

			Executed = true;
		}

		public override string ToString()
		{
			switch (Type)
			{
				case VoteType.Ban:
					return $"封禁 {Target}";
				case VoteType.Kick:
					return $"驱逐 {Target}";
				case VoteType.Kill:
					return $"杀死 {Target}";
				case VoteType.Mute:
					return $"禁言 {Target}";
				case VoteType.Command:
					return Target;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
