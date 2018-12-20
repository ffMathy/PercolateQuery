using Nest;

namespace PercolateQuery.IntegrationTests
{
    public class EsStockItem : IPercolatedDocument
    {
        public string Type { get; set; }
        public QueryContainer Query { get; set; }

        public string Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }

        public EsStockItem()
        {
            Type = "esstockitem";
        }
    }
}
