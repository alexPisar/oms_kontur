using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class RefShoppingStoreConfiguration : EntityTypeConfiguration<RefShoppingStore>
    {
        public RefShoppingStoreConfiguration()
        {
            this
                .HasKey(s => new { s.BuyerGln, s.MainGln })
                .ToTable("REF_SHOPPING_STORES", "EDI");

            this
                .Property(s => s.BuyerGln)
                .HasColumnName(@"BUYER_GLN")
                .HasMaxLength(16);

            this
                .Property(s => s.MainGln)
                .HasColumnName(@"MAIN_GLN")
                .HasMaxLength(16);

            OnCreated();
        }
        partial void OnCreated();
    }
}
