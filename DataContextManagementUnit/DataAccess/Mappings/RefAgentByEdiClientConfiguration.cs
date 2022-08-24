using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class RefAgentByEdiClientConfiguration : EntityTypeConfiguration<RefAgentByEdiClient>
    {
        public RefAgentByEdiClientConfiguration()
        {
            this
                .HasKey(r => new { r.Gln, r.IdAgent })
                .ToTable("REF_AGENTS_BY_EDI_CLIENTS", "EDI");

            this
                .Property(r => r.Gln)
                .HasColumnName(@"GLN")
                .HasMaxLength(16);

            this
                .Property(r => r.IdAgent)
                .HasColumnName(@"ID_AGENT");

            this
                .Property(r => r.AddedDate)
                .HasColumnName(@"DATE_ADDED");

            OnCreated();
        }

        partial void OnCreated();
    }
}
