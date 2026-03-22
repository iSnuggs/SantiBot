using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SantiBot.Db.Models;

public class XpCurrencyRewardEntityConfiguration : IEntityTypeConfiguration<XpCurrencyReward>
{
    public void Configure(EntityTypeBuilder<XpCurrencyReward> builder)
    {
        builder.HasIndex(x => new
               {
                   x.Level,
                   x.XpSettingsId
               })
               .IsUnique();
    }
}