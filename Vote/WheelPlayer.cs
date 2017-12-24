using System;
using Microsoft.Xna.Framework;
using TShockAPI;
using TShockAPI.DB;

namespace Vote
{
	internal class WheelPlayer : TSPlayer
	{
	    private const string WheelName = "Wheel";
	    
	    public WheelPlayer() : base(WheelName)
		{
			Group = VotePlugin.Config.ExecutiveGroup;
			Account = new UserAccount { Name = WheelName };
		}

		public override void SendErrorMessage(string msg)
		{
			TShock.Log.ConsoleError("[Vote] Result: {0}", msg);
		}

		public override void SendInfoMessage(string msg)
		{
			TShock.Log.ConsoleInfo("[Vote] Result: {0}", msg);
		}

		public override void SendSuccessMessage(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("[Vote] Result: {0}", msg);
			Console.ResetColor();

			TShock.Log.Info("[Vote] Result: {0}", msg);
		}

		public override void SendWarningMessage(string msg)
		{
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine("[Vote] Result: {0}", msg);
			Console.ResetColor();

			TShock.Log.Warn("[Vote] Result: {0}", msg);
		}

		public override void SendMessage(string msg, Color color)
		{
			SendMessage(msg, color.R, color.G, color.B);
		}

		public override void SendMessage(string msg, byte red, byte green, byte blue)
		{
			TShock.Log.ConsoleInfo(msg);
		}
	}
}
