using System;
using System.Diagnostics.CodeAnalysis;
using TShockAPI;
using TShockAPI.DB;

namespace Vote {
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal class TSWheelPlayer:TSPlayer {
		public TSWheelPlayer() : base("民意") {
			Group = VotePlugin.Config.ExecutiveGroup;
			User = new User { Name = "民意" };
		}

		public override void SendErrorMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"[投票] 指令返回: {msg}");
			Console.ResetColor();
		}

		public override void SendInfoMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"[投票] 指令返回: {msg}");
			Console.ResetColor();
		}

		public override void SendSuccessMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[投票] 指令返回: {msg}");
			Console.ResetColor();
		}

		public override void SendWarningMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine($"[投票] 指令返回: {msg}");
			Console.ResetColor();
		}

		public override void SendMessage(string msg, Color color) {
			SendMessage(msg, color.R, color.G, color.B);
		}

		public override void SendMessage(string msg, byte red, byte green, byte blue) {
			Console.WriteLine($"[投票] 指令返回: {msg}");
		}
	}
}
