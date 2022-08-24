using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefContractorAgentConfiguration : EntityTypeConfiguration<RefContractorAgent>
    {
        public RefContractorAgentConfiguration()
        {
            this
                .HasKey( p => new { p.IdContractor, p.IdAgent, p.IdManufacturer } )
                .ToTable( "REF_CONTRACTORS_AGENTS", "ABT" );

            this
                .Property( p => p.IdContractor )
                    .HasColumnName( @"ID_CONTRACTOR" );

            this
                .Property( p => p.IdManufacturer )
                    .HasColumnName( @"ID_MANUFACTURER" );

            this
                .Property( p => p.IdAgent )
                    .HasColumnName( @"ID_AGENT" );

            this
                .Property( p => p.StartDate )
                    .HasColumnName( @"START_DATE" );

            this
                .Property( p => p.EndDate )
                    .HasColumnName( @"END_DATE" );

            this
                .Property( p => p.SetUser )
                    .HasColumnName( @"SET_USER" )
                    .HasMaxLength( 128 )
                    .IsUnicode( false );

            this
                .Property( p => p.SetDateTime )
                    .HasColumnName( @"SET_DATETIME" );

            OnCreated();
        }

        partial void OnCreated();
    }
}
