using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TShockAPI;

namespace Vote {
	public class Vote {
		public DateTime Time { get; set; }
		public string Sponsor { get; set; }
		public string Target { get; set; }
		public VoteType Type { get; set; }
		public string Reason { get; set; }
		public bool Succeed { get; set; } = false;
		public bool Executed { get; set; } = false;
		public readonly List<string> Proponents = new List<string>();
		public readonly List<string> Opponents = new List<string>();
		public readonly List<string> Neutrals = new List<string>();

		public Vote(string sponsor, string target, VoteType type) {
			if(string.IsNullOrWhiteSpace(sponsor))
				throw new ArgumentNullException(nameof(sponsor));
			if(string.IsNullOrWhiteSpace(target))
				throw new ArgumentNullException(nameof(target));

			Sponsor = sponsor;
			Target = target;
			Type = type;
			Time = DateTime.UtcNow;
		}

		public Vote(string sponsor, string target, DateTime time, VoteType type) : this(sponsor, target, type) {
			Time = time;
		}

		public bool CheckPass() {
			Succeed = Proponents.Count > (Proponents.Count + Opponents.Count + Neutrals.Count) / 2;
			return Succeed;
		}

		[SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
		public void Execute() {
			if(!Succeed)
				return;

			switch(Type) {
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

		public override string ToString() {
			switch(Type) {
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
