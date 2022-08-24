using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class MapGoodManufacturerConfiguration : EntityTypeConfiguration<MapGoodManufacturer>
    {
        public MapGoodManufacturerConfiguration()
        {
            this
                .HasKey(p => p.IdGood)
                .ToTable("MAP_GOODS_MANUFACTURERS", "EDI");

            this
                .Property(p => p.IdGood)
                    .HasColumnName(@"ID_GOOD");

            this
                .Property(p => p.IdManufacturer)
                    .HasColumnName(@"ID_MANUFACTURER");

            this
                .Property(p => p.Name)
                    .HasColumnName(@"NAME")
                    .HasMaxLength(550)
                    .IsUnicode(true);

            OnCreated();
        }

        partial void OnCreated();
    }
}
