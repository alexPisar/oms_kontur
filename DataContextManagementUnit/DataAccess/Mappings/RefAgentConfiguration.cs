using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefAgentConfiguration : EntityTypeConfiguration<RefAgent>
    {
        public RefAgentConfiguration()
        {
            this
                .HasKey(p => new { p.Id })
                .ToTable("REF_AGENTS", "ABT");

            this
                .Property(p => p.Id)
                .HasColumnName(@"ID")
                .IsRequired();

            this
                .Property(p => p.Name)
                .HasColumnName(@"NAME")
                .HasMaxLength(100)
                .IsUnicode(false);

            this
                .Property(p => p.Description)
                .HasColumnName(@"DESCRIPTION")
                .HasMaxLength(300)
                .IsUnicode(false);

            OnCreated();
        }

        partial void OnCreated();
    }
}
