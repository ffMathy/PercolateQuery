using Nest;

namespace PercolateQuery.IntegrationTests
{
    public class EsStockItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }

        public EsStockItem()
        {
        }
    }
}
