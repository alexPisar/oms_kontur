using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class MapGoodByBuyerConfiguration : EntityTypeConfiguration<MapGoodByBuyer>
    {
        public MapGoodByBuyerConfiguration()
        {
            this
                .HasKey( p => new { p.Gln, p.IdMapGood } )
                .ToTable( "MAP_GOODS_BY_BUYERS", "EDI" );

            this
                .Property( p => p.Gln )
                .HasColumnName( @"GLN" )
                .HasMaxLength( 16 );

            this
                .Property( p => p.IdMapGood )
                .HasColumnName( @"ID_MAP_GOOD" )
                .HasMaxLength( 36 )
                .IsUnicode( false );

            OnCreated();
        }

        partial void OnCreated();
    }
}
