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
            elasticClient.DeleteIndex(Strings.SearchAgentIndexName);

            elasticClient.CreateIndex(Strings.StockItemIndexName, i => i
                .Mappings(map => map
                    .Map<EsStockItem>(m => m.AutoMap())));

            elasticClient.CreateIndex(Strings.SearchAgentIndexName, i => i
                .Mappings(map => map
                    .Map<EsSearchAgent>(m => m
                        .Properties(props => props
                            .Percolator(p => p
                                .Name(n => n.Query)
                            )))));

            elasticClient.IndexDocument(
                new EsStockItem
                {
                    Id = "1",
                    Name = "tesla",
                    Price = 100
                });
        }

        [Test]
        public async Task MappingIsCreatedCorrectly()
        {
            var mappingResponse = await _elasticClient.GetMappingAsync<EsSearchAgent>(m => m);

            mappingResponse.IsValid.ShouldBe(true);

            var indexProperties = IndexProperties(mappingResponse);
            var queryField = indexProperties["query"];
            queryField.Type.ShouldBe("percolator");
        }

        [Test]
        public async Task RegisterPriceAlertQuery()
        {
            var elasticClient = _elasticClient;

            var registered = await new PricesAlertService(elasticClient).Register(100, "tesla");
            registered.IsValid.ShouldBe(true);

            var getDocument = await elasticClient.GetAsync<EsSearchAgent>("document_with_alert");

            var queryVisitor = new DidWeVisitProperQueries { };
            getDocument.Source.Query.Accept(queryVisitor);

            queryVisitor.NumericRangeQuery.LessThanOrEqualTo.ShouldBe(100);
            queryVisitor.MatchQuery.Query.ShouldBe("tesla");
        }

        [Test]
        public async Task UpdatingTeslaItemTo90ShouldRiseAlert()
        {
            //register alert
            var registered = await new PricesAlertService(_elasticClient).Register(100, "tesla");

            var @event = new ShoppingItemUpdated { Id = 1, Name = "tesla", Price = 90 };

            await new UpdateShoppingItemInElasticSearchHandler(_elasticClient).Handle(@event);

            var updatedDocument = await _elasticClient.GetAsync<EsStockItem>(@event.Id.ToString());
            updatedDocument.Source.Price.ShouldBe(90);

            var alertsFound = await new CheckAlertsHandler(_elasticClient).Handle(@event);

            alertsFound.ShouldBe(true);
        }

        [Test]
        public async Task UpdatingTeslaItemTo110ShouldNotRiseAlert()
        {
            //register alert
            var registered = await new PricesAlertService(_elasticClient).Register(100, "tesla");

            var @event = new ShoppingItemUpdated { Id = 1, Name = "tesla", Price = 110 };

            await new UpdateShoppingItemInElasticSearchHandler(_elasticClient).Handle(@event);

            var updatedDocument = await _elasticClient.GetAsync<EsStockItem>(@event.Id.ToString());
            updatedDocument.Source.Price.ShouldBe(110);

            var alertsFound = await new CheckAlertsHandler(_elasticClient).Handle(@event);

            alertsFound.ShouldBe(false);
        }

        private IProperties IndexProperties(IGetMappingResponse mappingResponse)
        {
            var indexMapping = mappingResponse.Indices.FirstOrDefault();
            return indexMapping.Value.Mappings.Values.FirstOrDefault().Properties;
        }
    }

    public class CheckAlertsHandler
    {
        private readonly ElasticClient _elasticClient;

        public CheckAlertsHandler(ElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public Task<bool> Handle(ShoppingItemUpdated @event)
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
