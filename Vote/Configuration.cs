using System;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace Vote
{
	[JsonObject(MemberSerialization.OptIn)]
	internal class Configuration
	{
		public static readonly string FilePath = Path.Combine(TShock.SavePath, "vote.json");

		[JsonProperty("ExecutiveGroup")]
		public string ExecutiveGroupName = "wheel";

		public Group ExecutiveGroup { get; private set; }

		[JsonProperty]
		public int MaxAwaitingVotingTime = 60;

		[JsonProperty]
		public int MaxAwaitingReasonTime = 30;

		[JsonProperty]
		public bool ShowResult = true;

		internal void LoadGroup()
		{
			ExecutiveGroup = TShock.Groups.GetGroupByName(ExecutiveGroupName);
			if (ExecutiveGroup == null)
			{
				if (!ExecutiveGroupName.Equals("wheel", StringComparison.Ordinal))
					TShock.Log.ConsoleError("[Vote] Group wheel will be used as {0} doesn't exist.", ExecutiveGroupName);

				LoadWheelGroup();
			}
		}

		private void LoadWheelGroup()
		{
			ExecutiveGroup = TShock.Groups.GetGroupByName("wheel");

			if (ExecutiveGroup != null)
				return;

			TShock.Groups.AddGroup("wheel", Group.DefaultGroup.Name,
				string.Join(",", Permissions.ban, Permissions.kick, Permissions.mute, Permissions.kill),
				Group.defaultChatColor
			);

			ExecutiveGroup = TShock.Groups.GetGroupByName("wheel");
		}

		public static Configuration Read(string path)
		{
			if (!File.Exists(path))
				return new Configuration();
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var sr = new StreamReader(fs))
				{
					var cf = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
					return cf;
				}
			}
		}

		public void Write(string path)
		{
			using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				var str = JsonConvert.SerializeObject(this, Formatting.Indented);
				using (var sw = new StreamWriter(fs))
				{
					sw.Write(str);
				}
			}
		}
	}
}
