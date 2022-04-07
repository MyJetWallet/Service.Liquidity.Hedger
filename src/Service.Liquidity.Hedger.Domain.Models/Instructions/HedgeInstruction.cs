using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeInstruction
    {
        [DataMember(Order = 1)] public string TargetAssetSymbol { get; set; }
        [DataMember(Order = 2)] public List<HedgePairAsset> PairAssets { get; set; } = new();
        [DataMember(Order = 3)] public decimal TargetVolume { get; set; }
        [DataMember(Order = 4)] public string MonitoringRuleId { get; set; }
        [DataMember(Order = 5)] public HedgeInstructionStatus Status { get; set; }
        [DataMember(Order = 6)] public DateTime Date { get; set; } = DateTime.UtcNow;
        [DataMember(Order = 7)] public decimal Weight { get; set; }

        public bool Validate(out ICollection<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TargetAssetSymbol))
            {
                errors.Add($"{nameof(TargetAssetSymbol)} are empty");
            }

            if (!PairAssets.Any())
            {
                errors.Add($"{nameof(PairAssets)} are empty");
            }

            if (TargetVolume <= 0)
            {
                errors.Add($"{nameof(TargetVolume)} must be bigger than 0");
            }

            return !errors.Any();
        }
    }
}