using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class ExcelColumnConfiguration : IEntityTypeConfiguration<ExcelColumn>
{
    public void Configure(EntityTypeBuilder<ExcelColumn> builder)
    {
        builder.HasKey(e => e.Id);

        // Veritabanı kolon adını korumak için
        builder.Property(e => e.ColumnTypeId).HasColumnName("ColumnType");
    }
}
