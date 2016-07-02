using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace Vote {
	[JsonObject(MemberSerialization.OptIn)]
	internal class Configuration {
		public static readonly string FilePath = Path.Combine(TShock.SavePath, "vote.json");

		[JsonProperty("执行组")]
		public string ExecutiveGroupName = "wheel";

		public Group ExecutiveGroup { get; private set; }

		[JsonProperty("单次投票持续时间")]
		public int MaxAwaitingVotingTime = 60;

		[JsonProperty("单次原因等待时间")]
		public int MaxAwaitingReasonTime = 30;

		[JsonProperty("显示玩家投票结果")]
		public bool ShowResult = true;

		internal void LoadGroup() {
			ExecutiveGroup = TShock.Groups.GetGroupByName(ExecutiveGroupName);
			if(ExecutiveGroup == null) {
				if(ExecutiveGroupName == "wheel")
					LoadWheelGroup();
				else {
					TShock.Log.ConsoleError("[投票] 组 {0} 不存在, 插件将使用默认组.", ExecutiveGroupName);
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
