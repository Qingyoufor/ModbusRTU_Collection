using Server.Infrastructure.Data;
using Server.Models.Common;
using Server.Models.Entities;

namespace Server.Infrastructure.Repositories
{
    public class ProductionLineRepository : RepositoryBase<ProductionLine>,IProductionLineRepository
    {
        public ProductionLineRepository(SmartFactoryDbContext context) : base(context) { }
    }
}
