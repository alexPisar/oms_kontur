using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class ConnStringConfiguration : EntityTypeConfiguration<ConnString>
    {
        public ConnStringConfiguration()
        {
			this.HasKey( p => p.ConnStr )
				.ToTable( "CONN_STRINGS", "EDI" );
			;

			this
				.Property( p => p.ConnStr )
				.HasColumnName( @"CONN_STRING" )
				.IsRequired()
				.HasMaxLength( 1024 )
				.IsUnicode( false );
			this
				.Property( p => p.Comment )
				.HasColumnName( @"COMMENT" )
				.IsRequired()
				.HasMaxLength( 128 )
				.IsUnicode( false );

			OnCreated();
        }

        partial void OnCreated();

    }
}
