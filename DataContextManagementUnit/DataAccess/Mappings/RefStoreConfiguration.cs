using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefStoreConfiguration : EntityTypeConfiguration<RefStore>
    {
        public RefStoreConfiguration()
        {
            this
                .HasKey(p => p.Id)
                .ToTable("REF_STORES", "ABT");

            this
                .Property(p => p.Id)
                    .HasColumnName(@"ID")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.Name)
                    .HasColumnName(@"NAME")
                    .HasMaxLength(128)
                    .IsUnicode(false);

            this
                .Property(p => p.IdInstance)
                .HasColumnName(@"ID_INSTANCE");

            this
                .Property(p => p.IsReal)
                .HasColumnName(@"IS_REAL")
                .IsRequired()
                .HasMaxLength(1);

            this
                .Property(p => p.IsConsignment)
                .HasColumnName(@"IS_CONSIGNMENT")
                .IsRequired()
                .HasMaxLength(1);

            this
                .Property(p => p.OldId)
                .HasColumnName(@"OLDID");

            this
                .Property(p => p.SortId)
                .HasColumnName(@"SORTID");

            this
                .Property(p => p.Close)
                .HasColumnName(@"CLOSE")
                .IsRequired()
                .HasMaxLength(1);

            this
                .Property(p => p.IdStoreClass)
                .HasColumnName(@"ID_STORE_CLASS");

            OnCreated();
        }

        partial void OnCreated();
    }
}
