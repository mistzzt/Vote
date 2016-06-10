using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace Vote {
	public class VotePlugin:TerrariaPlugin {
		public const string VotePlayerData = "votedata";

		public override string Name => "Vote";
		public override string Author => "MistZZT";
		public override string Description => "Gives players the right to vote in Terraria server running TShock.";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public VotePlugin(Main game) : base(game) { }

		public override void Initialize() {
			ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
		}

		protected override void Dispose(bool disposing) {
			if(disposing) {
				ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
			}
			base.Dispose(disposing);
		}

		private void OnJoin(JoinEventArgs args)
			=> TShock.Players[args.Who]?.SetData(VotePlayerData, new PlayerData());

		private void OnLeave(LeaveEventArgs args)
			=> TShock.Players[args.Who]?.RemoveData(VotePlayerData);
	}
}
