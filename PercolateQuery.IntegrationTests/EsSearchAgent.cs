using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace PercolateQuery.IntegrationTests
{
    public class EsSearchAgent : IPercolatedDocument
    {
        public string Type { get; set; }
        public QueryContainer Query { get; set; }

        public EsSearchAgent()
        {
            Type = "essearchagent";
        }
    }
}
