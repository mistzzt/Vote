using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vote {
	public class Vote {
		public DateTime Time { get; set; }
		public string Sponsor { get; set; }
		public string Target { get; set; }
		public VoteType Type { get; set; }
		public string Reason { get; set; }
		public bool Succeed { get; set; } = false;
		public bool Executed { get; set; } = false;
		public readonly List<string> Proponents = new List<string>();
		public readonly List<string> Opponents = new List<string>();

		public Vote(string sponsor, string target) {
			if(string.IsNullOrWhiteSpace(sponsor))
				throw new ArgumentNullException(nameof(sponsor));
			if(string.IsNullOrWhiteSpace(target))
				throw new ArgumentNullException(nameof(target));

			Sponsor = sponsor;
			Target = target;
			Time = DateTime.UtcNow;
		}

		public Vote(string sponsor, string target, DateTime time) : this(sponsor, target) {
			Time = time;
		}
	}
}
