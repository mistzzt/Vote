namespace Vote {
	public class PlayerData {
		public Vote StartedVote;
		public bool Support = false;
		public bool Voted = false;
		public bool AwaitingReason = false;
		public bool AwaitingConfirm = false;
		public int ToVoteMsgCd = 0;
	}
}
