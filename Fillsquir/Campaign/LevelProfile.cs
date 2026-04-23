using System.Collections.Generic;

namespace Fillsquir.Campaign
{
    public sealed class LevelProfile
    {
        public int Level { get; set; }
        public int? Fragments { get; set; }
        public double? SnapMultiplier { get; set; }
        public string AnchorMode { get; set; } = "none";
        public bool EnableHint { get; set; }
        public bool SingleUseGhostHint { get; set; }
        public int? TimeLimitSeconds { get; set; }
        public bool? TimeTrialOptional { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class LevelProfiles
    {
        public int Version { get; set; }
        public List<LevelProfile> Profiles { get; set; } = new();
        public LevelProfile? ForLevel(int level) => Profiles?.Find(p => p.Level == level);
    }
}
