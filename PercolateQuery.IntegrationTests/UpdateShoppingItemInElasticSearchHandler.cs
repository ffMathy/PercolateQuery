using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;

namespace PercolateQuery.IntegrationTests
{
    public class UpdateShoppingItemInElasticSearchHandler
    {
        private readonly ElasticClient _elasticClient;

        public UpdateShoppingItemInElasticSearchHandler(ElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task Handle(EsStockItem @event)
        {
            var updateResponse = await _elasticClient.IndexAsync<EsStockItem>(@event, x => x);
            await _elasticClient.RefreshAsync(Strings.StockItemIndexName);
        }
    }
}