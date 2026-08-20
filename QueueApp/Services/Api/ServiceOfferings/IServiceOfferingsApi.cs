using Refit;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Services.Api.ServiceOfferings;

public interface IServiceOfferingsApi
{
    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/services?select=*&order=sort_order.asc")]
    Task<List<ServiceResponse>> GetServicesAsync([AliasAs("business_id")] string businessIdEq);

    [Post("/services")]
    Task<List<ServiceResponse>> CreateServiceAsync([Body] CreateServiceRequest request);

    [Patch("/services")]
    Task UpdateServiceAsync([AliasAs("id")] string idEq, [Body] UpdateServiceRequest request);

    [Patch("/services")]
    Task SetServiceActiveAsync([AliasAs("id")] string idEq, [Body] SetServiceActiveRequest request);
}
