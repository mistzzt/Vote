using TShockAPI;

namespace Vote {
	public class PlayerData {
		public int Index;
		public Vote StartedVote;
		public Vote OngoingVote;
		public bool Support = false;
		public int ToVoteMsgCd = 0;

		private bool _vote;
		private bool _confirm;
		private bool _reason;

		public bool AwaitingVote {
			get { return _vote; }
			set {
				VotePlugin.Utils.AwaitVote(TShock.Players[Index], !value);
				_vote = value;
			}
		}

		public bool AwaitingConfirm {
			get { return _confirm; }
			set {
				VotePlugin.Utils.AwaitConfirm(TShock.Players[Index], !value);
				_confirm = value;
			}
		}

		public bool AwaitingReason {
			get { return _reason; }
			set {
				VotePlugin.Utils.AwaitReason(TShock.Players[Index], !value);
				_reason = value;
			}
		}

		public PlayerData(int index) {
			Index = index;
		}
	}
}
