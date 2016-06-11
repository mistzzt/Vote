using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace Vote {
	[JsonObject(MemberSerialization.OptIn)]
	internal class Configuration {
		public static readonly string FilePath = Path.Combine(TShock.SavePath, "vote.json");

		[JsonProperty("ExecutiveGroup")]
		public string ExecutiveGroupName = "wheel";

		public Group ExecutiveGroup { get; private set; }

		[JsonProperty]
		public int MaxAwaitingVotingTime = 15;

		[JsonProperty]
		public int MaxAwaitingReasonTime = 15;

		[JsonProperty]
		public int MaxAwaitingConfirmTime = 15;

		[JsonProperty]
		public int WarningInterval = 1;

		public Configuration() {
			ExecutiveGroup = TShock.Groups.GetGroupByName(ExecutiveGroupName);
			if(ExecutiveGroup == null) {
				if(ExecutiveGroupName == "wheel")
					LoadWheelGroup();
				else {
					TShock.Log.Error("[Vote] Group {0} doesn't exist. Group wheel will be used", ExecutiveGroupName);
					LoadWheelGroup();
				}
			}
		}

		private void LoadWheelGroup() {
			ExecutiveGroup = TShock.Groups.GetGroupByName("wheel");
			if(ExecutiveGroup != null)
				return;

			TShock.Groups.AddGroup("wheel", Group.DefaultGroup.Name,
				string.Join(",", Permissions.ban, Permissions.kick, Permissions.mute, Permissions.kill),
				Group.defaultChatColor
				);
			ExecutiveGroup = TShock.Groups.GetGroupByName("wheel");
			Debug.Assert(ExecutiveGroup != null, "Wheel Group is null, wheras it was added.");
		}

		public static Configuration Read(string path) {
			if(!File.Exists(path))
				return new Configuration();
			using(var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using(var sr = new StreamReader(fs)) {
					var cf = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
					return cf;
				}
			}
		}

		public void Write(string path) {
			using(var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)) {
				var str = JsonConvert.SerializeObject(this, Formatting.Indented);
				using(var sw = new StreamWriter(fs)) {
					sw.Write(str);
				}
			}
		}
	}
}
