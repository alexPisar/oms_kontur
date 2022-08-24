using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefDistrictConfiguration : EntityTypeConfiguration<RefDistrict>
    {
        public RefDistrictConfiguration()
        {
            this
                .HasKey( d => d.Id )
                .ToTable( "REF_DISTRICTS", "ABT" );

            this
                .Property( d => d.Id )
                .HasColumnName( "ID" )
                .IsRequired()
                .HasDatabaseGeneratedOption( System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None );

            this
                .Property( d => d.Name )
                .HasColumnName( "NAME" )
                .HasMaxLength( 256 )
                .IsUnicode( false );

            OnCreated();
        }

        partial void OnCreated();
    }
}
