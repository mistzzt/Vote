using System;
using System.Diagnostics.CodeAnalysis;
using TShockAPI;
using TShockAPI.DB;

namespace Vote {
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal class TSWheelPlayer:TSPlayer {
		public TSWheelPlayer() : base("Wheel") {
			Group = VotePlugin.Config.ExecutiveGroup;
			User = new User { Name = "Wheel" };
		}

		public override void SendErrorMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"[Vote] Command executing result: {msg}");
			Console.ResetColor();
		}

		public override void SendInfoMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"[Vote] Command executing result: {msg}");
			Console.ResetColor();
		}

		public override void SendSuccessMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[Vote] Command executing result: {msg}");
			Console.ResetColor();
		}

		public override void SendWarningMessage(string msg) {
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine($"[Vote] Command executing result: {msg}");
			Console.ResetColor();
		}

		public override void SendMessage(string msg, Color color) {
			SendMessage(msg, color.R, color.G, color.B);
		}

		public override void SendMessage(string msg, byte red, byte green, byte blue) {
			Console.WriteLine($"[Vote] Command executing result: {msg}");
		}
	}
}
