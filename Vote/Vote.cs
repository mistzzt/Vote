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

		public bool Succeed { get; set; }

		public bool Executed { get; set; }

		public readonly List<string> Proponents = new List<string>();

		public readonly List<string> Opponents = new List<string>();

		public readonly List<string> Neutrals = new List<string>();

		private Timer _voteTimer;

		public Vote(TSPlayer player, string target, VoteType type)
		{
			Sponsor = player.User?.Name ?? player.Name;
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

		public bool CheckPass()
		{
			Succeed = Proponents.Count > (Proponents.Count + Opponents.Count + Neutrals.Count) / 2;
			return Succeed;
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
					return $"Banning {Target}";
				case VoteType.Kick:
					return $"Kicking {Target}";
				case VoteType.Kill:
					return $"Killing {Target}";
				case VoteType.Mute:
					return $"Muting {Target}";
				case VoteType.Command:
					return Target;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
