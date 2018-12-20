using Nest;

namespace PercolateQuery.IntegrationTests
{
    public interface IPercolatedDocument
    {
        string Type { get; set; }
        QueryContainer Query { get; set; }
    }
}