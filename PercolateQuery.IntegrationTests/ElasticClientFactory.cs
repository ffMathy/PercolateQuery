﻿using System;
using Nest;

namespace PercolateQuery.IntegrationTests
{
    public class ElasticClientFactory
    {
        public static ElasticClient ElasticClient()
        {
            var uri = new Uri("http://localhost:9201");
            var settings = new ConnectionSettings(uri);

            settings.DefaultMappingFor<EsSearchAgent>(x => x.IndexName(Strings.SearchAgentIndexName));
            settings.DefaultMappingFor<EsStockItem>(x => x.IndexName(Strings.StockItemIndexName));

            settings.DisableDirectStreaming();
            settings.EnableDebugMode();
            var elasticClient =
                new ElasticClient(settings);

            return elasticClient;
        }
    }
}