﻿using System.Threading.Tasks;
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

        public async Task Handle(ShoppingItemUpdated @event)
        {
            var updateResponse = await _elasticClient.UpdateAsync<EsStockItem>(@event.Id.ToString(),
                u => u
                    .Doc(new EsStockItem {Name = @event.Name, Price = @event.Price})
                    .Refresh(Refresh.WaitFor));
        }
    }
}