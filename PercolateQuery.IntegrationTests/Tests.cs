using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Nest;
using NUnit.Framework;
using Shouldly;

namespace PercolateQuery.IntegrationTests
{
    public class Tests
    {
        private ElasticClient _elasticClient => ElasticClientFactory.ElasticClient();

        [OneTimeSetUp]
        public void SetUpFixture()
        {
            var elasticClient = _elasticClient;

            elasticClient.DeleteIndex(Strings.StockItemIndexName);

            elasticClient.CreateIndex(Strings.StockItemIndexName, i => i
                .Mappings(map => map
                    .Map<EsStockItem>(m => m.AutoMap())));

            elasticClient.IndexDocument(
                new EsStockItem
                {
                    Id = "1",
                    Name = "tesla",
                    Price = 100
                });
        }

        [Test]
        public async Task UpdatingTeslaItemTo90ShouldRiseAlert()
        {
            //register alert
            var registered = await new PricesAlertService(_elasticClient).Register(100, "tesla");

            var @event = new EsStockItem { Id = Guid.NewGuid().ToString(), Name = "tesla", Price = 90 };

            await new UpdateShoppingItemInElasticSearchHandler(_elasticClient).Handle(@event);

            var updatedDocument = await _elasticClient.GetAsync<EsStockItem>(@event.Id.ToString());
            updatedDocument.Source.Price.ShouldBe(90);

            var alertsFound = await new CheckAlertsHandler(_elasticClient).Handle(@event);
            alertsFound.Documents.Any().ShouldBe(true);
        }

        [Test]
        public async Task UpdatingTeslaItemTo110ShouldNotRiseAlert()
        {
            //register alert
            var registered = await new PricesAlertService(_elasticClient).Register(100, "tesla");

            var @event = new EsStockItem { Id = Guid.NewGuid().ToString(), Name = "tesla", Price = 110 };

            await new UpdateShoppingItemInElasticSearchHandler(_elasticClient).Handle(@event);

            var updatedDocument = await _elasticClient.GetAsync<EsStockItem>(@event.Id);
            updatedDocument.Source.Price.ShouldBe(110);

            var alertsFound = await new CheckAlertsHandler(_elasticClient).Handle(@event);
            alertsFound.Documents.Any().ShouldBe(false);
        }
    }

    public class CheckAlertsHandler
    {
        private readonly ElasticClient _elasticClient;

        public CheckAlertsHandler(ElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public Task<ISearchResponse<EsSearchAgent>> Handle(EsStockItem @event)
        {
            return new PricesAlertService(_elasticClient).Percolate(@event);
        }
    }

    public class UpdateShoppingItemCommand
    {
        public string Name { get; set; }
        public double Price { get; set; }
    }

    class DidWeVisitProperQueries : QueryVisitor
    {
        public INumericRangeQuery NumericRangeQuery { get; set; }
        public IMatchQuery MatchQuery { get; set; }

        public override void Visit(INumericRangeQuery query)
        {
            NumericRangeQuery = query;
            base.Visit(query);
        }
        public override void Visit(IMatchQuery query)
        {
            MatchQuery = query;
            base.Visit(query);
        }
    }
}
