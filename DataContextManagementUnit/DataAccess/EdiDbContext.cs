using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
	public partial class EdiDbContext : DbContext
    {
        #region Constructors

        /// <summary>
        /// Initialize a new EdiDbContext object.
        /// </summary>
        public EdiDbContext() :
                base(GetDefaultConnection(), true)
        {
            Configure();
        }
		

        /// <summary>
        /// Initializes a new EdiDbContext object using the connection string found in the 'EdiDbContext' section of the application configuration file.
        /// </summary>
        public EdiDbContext(string nameOrConnectionString) :
                base(nameOrConnectionString)
        {
            Configure();
        }

        /// <summary>
        /// Initialize a new EdiDbContext object.
        /// </summary>
        public EdiDbContext(DbConnection existingConnection, bool contextOwnsConnection) :
                base(existingConnection, contextOwnsConnection)
        {
            Configure();
        }

        /// <summary>
        /// Initialize a new EdiDbContext object.
        /// </summary>
        public EdiDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext) :
                base(objectContext, dbContextOwnsObjectContext)
        {
            Configure();
        }

        /// <summary>
        /// Initialize a new EdiDbContext object.
        /// </summary>
        public EdiDbContext(string nameOrConnectionString, DbCompiledModel model) :
                base(nameOrConnectionString, model)
        {
            Configure();
        }

        /// <summary>
        /// Initialize a new EdiDbContext object.
        /// </summary>
        public EdiDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) :
                base(existingConnection, model, contextOwnsConnection)
        {
            Configure();
        }

        private void Configure()
        {
            this.Configuration.AutoDetectChangesEnabled = true;
            this.Configuration.LazyLoadingEnabled = true;
            this.Configuration.ProxyCreationEnabled = true;
            this.Configuration.ValidateOnSaveEnabled = true;
            this.Database.CommandTimeout = 180;
        }


        #endregion

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new Mapping.MapGoodConfiguration());
            modelBuilder.Configurations.Add(new Mapping.MapPriceTypeConfiguration());
            modelBuilder.Configurations.Add(new Mapping.RefCompanyConfiguration());
            modelBuilder.Configurations.Add(new Mapping.DocLineItemConfiguration());
            modelBuilder.Configurations.Add(new Mapping.LogOrderConfiguration());
            modelBuilder.Configurations.Add(new Mapping.DocOrderConfiguration());
            modelBuilder.Configurations.Add(new Mapping.RefOrderStatusConfiguration());
            modelBuilder.Configurations.Add(new Mapping.ViewRefFilialConfiguration());
            modelBuilder.Configurations.Add(new Mapping.ViewRefContractorConfiguration());
            modelBuilder.Configurations.Add(new Mapping.ViewInvoicDetailConfiguration());
			modelBuilder.Configurations.Add( new Mapping.ViewInvoicHeadConfiguration() );
			modelBuilder.Configurations.Add( new Mapping.ConnStringConfiguration() );
            modelBuilder.Configurations.Add( new Mapping.ConnectedBuyersConfiguration() );
            modelBuilder.Configurations.Add( new Mapping.MapGoodByBuyerConfiguration() );
            modelBuilder.Configurations.Add( new Mapping.RefShoppingStoreConfiguration() );
            modelBuilder.Configurations.Add(new Mapping.MapGoodManufacturerConfiguration());
            modelBuilder.Configurations.Add(new Mapping.RefAgentByEdiClientConfiguration());

            CustomizeMapping(modelBuilder);
        }

        partial void CustomizeMapping(DbModelBuilder modelBuilder);

        public virtual DbSet<MapGood> MapGoods { get; set; }
        public virtual DbSet<MapPriceType> MapPriceTypes { get; set; }
        public virtual DbSet<RefCompany> RefCompanies { get; set; }
        public virtual DbSet<DocLineItem> DocLineItems { get; set; }
        public virtual DbSet<LogOrder> LogOrders { get; set; }
        public virtual DbSet<DocOrder> DocOrders { get; set; }
        public virtual DbSet<RefOrderStatus> RefOrderStatuses { get; set; }
        public virtual DbSet<ViewRefFilial> ViewRefFilials { get; set; }
        public virtual DbSet<ViewRefContractor> ViewRefContractors { get; set; }
        public virtual DbSet<ViewInvoicDetail> ViewInvoicDetails { get; set; }
		public virtual DbSet<ViewInvoicHead> ViewInvoicHeads { get; set; }
		public virtual DbSet<ConnString> ConnStrings { get; set; }
        public virtual DbSet<ConnectedBuyers> ConnectedBuyers { get; set; }
        public virtual DbSet<MapGoodByBuyer> MapGoodsByBuyers { get; set; }
        public virtual DbSet<RefShoppingStore> RefShoppingStores { get; set; }
        public virtual DbSet<MapGoodManufacturer> MapGoodsManufacturers { get; set; }
        public virtual DbSet<RefAgentByEdiClient> RefAgentsByEdiClients { get; set; }
    }
}
