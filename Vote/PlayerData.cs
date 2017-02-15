using TShockAPI;

namespace Vote
{
	public class PlayerData
	{
		public const string VotePlayerData = "votedata";

		public static PlayerData GetData(TSPlayer player)
		{
			var data = player.GetData<PlayerData>(VotePlayerData);
			if (data == null)
			{
				data = new PlayerData(player);
				player.SetData(VotePlayerData, data);
			}
			return data;
		}

		public TSPlayer Player { get; }

		public Vote StartedVote;

		public Vote OngoingVote;

		public bool Support = false;

		private bool _vote;
		private bool _confirm;
		private bool _reason;

		public bool AwaitingVote
		{
			get
			{
				return _vote;
			}
			set
			{
				Utils.AwaitVote(Player, !value);
				_vote = value;
			}
		}

		public bool AwaitingConfirm
		{
			get
			{
				return _confirm;
			}
			set
			{
				Utils.AwaitConfirm(Player, !value);
				_confirm = value;
			}
		}

		public bool AwaitingReason
		{
			get
			{
				return _reason;
			}
			set
			{
				Utils.AwaitReason(Player, !value);
				_reason = value;
			}
		}

		public PlayerData(TSPlayer player)
		{
			Player = player;
		}
	}
}
