using System.Linq;
using System.Threading.Tasks;
using Nest;

namespace PercolateQuery.IntegrationTests
{
    public class PricesAlertService
    {
        private readonly ElasticClient _elasticClient;

        public PricesAlertService(ElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<IIndexResponse> Register(double price, string itemName)
        {
            var query = new QueryContainerDescriptor<EsStockItem>()
                .Bool(b => b
                    .Must(
                        must => must.Match(m => m.Field(f => f.Name).Query(itemName)),
                        must => must.Range(r => r.Field(f => f.Price).LessThanOrEquals(price))));
            var indexResponse = await _elasticClient
                .IndexDocumentAsync(new EsSearchAgent() { Query = query });
            await _elasticClient.RefreshAsync(_elasticClient.ConnectionSettings.DefaultIndex);

            return indexResponse;
        }

        public async Task<bool> Percolate(ShoppingItemUpdated shoppingItemUpdated)
        {
            var esStockItem = new EsStockItem
            {
                Name = shoppingItemUpdated.Name,
                Price = shoppingItemUpdated.Price
            };
            var searchResponse = await _elasticClient.SearchAsync<EsSearchAgent>(s => s
                .Query(q => q.Percolate(p => p
                    .Field(f => f.Query)
                    .Routing(Routing.From(esStockItem))
                    .Index(Strings.StockItemIndexName)
                    .Document(esStockItem))));

            return searchResponse.Documents.Any();
        }
    }
}