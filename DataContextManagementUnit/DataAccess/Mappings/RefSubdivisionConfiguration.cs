using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefSubdivisionConfiguration : EntityTypeConfiguration<RefSubdivision>
    {
        public RefSubdivisionConfiguration()
        {
            this
                .HasKey(r => r.Id)
                .ToTable("REF_SUBDIVISIONS", "ABT");

            this
                .Property(r => r.Id)
                .HasColumnName(@"ID")
                .IsRequired();

            this
                .Property(r => r.Name)
                .HasColumnName(@"NAME");

            this
                .Property(r => r.OldId)
                .HasColumnName(@"OLDID");

            OnCreated();
        }

        partial void OnCreated();
    }
}
