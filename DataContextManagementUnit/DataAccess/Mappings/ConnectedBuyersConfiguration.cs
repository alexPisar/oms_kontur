using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class ConnectedBuyersConfiguration : EntityTypeConfiguration<ConnectedBuyers>
    {
        public ConnectedBuyersConfiguration()
        {
            this
                .HasKey( c => c.Gln )
                .ToTable( "CONNECTED_BUYERS", "EDI" );

            this
                .Property( c => c.Gln )
                    .HasColumnName( @"GLN" )
                    .IsRequired()
                    .HasMaxLength( 16 )
                    .IsUnicode( false );

            this
                .Property( c => c.OrderExchangeType )
                    .HasColumnName( @"ORDER_EXCHANGE_TYPE" );

            this
                .Property( c => c.ShipmentExchangeType )
                    .HasColumnName( @"SHIPMENT_EXCHANGE_TYPE" );

            this
                .Property(c => c.MultiDesadv)
                    .HasColumnName(@"MULTI_DESADV");

            this
                .Property(c => c.PriceIncludingVat)
                    .HasColumnName(@"PRICE_INCLUDING_VAT");

            this
                .Property(c => c.DocStatusSendDesadv)
                    .HasColumnName(@"DOC_STATUS_SEND_DESADV");

            this
                .Property(c => c.SendTomorrow)
                    .HasColumnName(@"SEND_TOMORROW");

            this
                .Property(c => c.PermittedToMatchingGoods)
                    .HasColumnName(@"PERMITTED_TO_MATCHING_GOODS");

            this
                .Property(c => c.ExportOrdersByManufacturers)
                    .HasColumnName(@"EXPORT_ORDERS_BY_MANUFACTURERS");

            this
                .Property(c => c.IncludedBuyerCodes)
                    .HasColumnName(@"INCLUDED_BUYER_CODES");

            this
                .Property(c => c.UseSplitDocProcedure)
                    .HasColumnName(@"USE_SPLIT_DOC_PROCEDURE");

            this
                .HasMany(c => c.ShoppingStores)
                .WithRequired(s => s.MainShoppingStore)
                .HasForeignKey(c => c.MainGln)
                .WillCascadeOnDelete(false);

            OnCreated();
        }

        partial void OnCreated();
    }
}
