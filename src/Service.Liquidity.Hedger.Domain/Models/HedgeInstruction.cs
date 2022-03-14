using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Service.Liquidity.Hedger.Domain.Models
{
    [DataContract]
    public class HedgeInstruction
    {
        [DataMember(Order = 1)] public string BaseAssetSymbol { get; set; }
        [DataMember(Order = 2)] public List<HedgeSellAssets> QuoteAssets { get; set; } = new();
        [DataMember(Order = 3)] public decimal TargetVolume { get; set; }

        public bool Validate(out ICollection<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(BaseAssetSymbol))
            {
                errors.Add($"{nameof(BaseAssetSymbol)} are empty");
            }

            if (!QuoteAssets.Any())
            {
                errors.Add($"{nameof(QuoteAssets)} are empty");
            }

            if (TargetVolume <= 0)
            {
                errors.Add($"{nameof(TargetVolume)} must be bigger than 0");
            }

            return !errors.Any();
        }
    }
}