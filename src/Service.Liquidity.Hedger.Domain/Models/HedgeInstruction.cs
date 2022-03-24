using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeInstruction
    {
        [DataMember(Order = 1)] public string TargetAssetSymbol { get; set; }
        [DataMember(Order = 2)] public List<HedgeSellAsset> SellAssets { get; set; } = new();
        [DataMember(Order = 3)] public decimal TargetVolume { get; set; }

        public bool Validate(out ICollection<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TargetAssetSymbol))
            {
                errors.Add($"{nameof(TargetAssetSymbol)} are empty");
            }

            if (!SellAssets.Any())
            {
                errors.Add($"{nameof(SellAssets)} are empty");
            }

            if (TargetVolume <= 0)
            {
                errors.Add($"{nameof(TargetVolume)} must be bigger than 0");
            }

            return !errors.Any();
        }
    }
}