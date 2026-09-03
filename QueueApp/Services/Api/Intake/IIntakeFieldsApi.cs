using Refit;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Services.Api.Intake;

// TODO: stub — every route here reads or writes service_intake_fields, which does not exist yet.
// Documentation/service-intake-fields-backend-requirements.md is what these are built against.
public interface IIntakeFieldsApi
{
    // The customer flow asks per business, not per service: it needs to know whether the service
    // the customer is about to pick has questions before they pick it, and one filtered read costs
    // less than one per service row.
    [Get("/service_intake_fields?select=*,service:services!inner(business_id)&order=sort_order.asc")]
    Task<List<IntakeFieldResponse>> GetFieldsForBusinessAsync(
        [AliasAs("service.business_id")] string businessIdEq);

    [Get("/service_intake_fields?select=*&order=sort_order.asc")]
    Task<List<IntakeFieldResponse>> GetFieldsForServiceAsync([AliasAs("service_id")] string serviceIdEq);

    [Post("/service_intake_fields")]
    Task<List<IntakeFieldResponse>> CreateFieldAsync([Body] CreateIntakeFieldRequest request);

    [Patch("/service_intake_fields")]
    Task UpdateFieldAsync([AliasAs("id")] string idEq, [Body] UpdateIntakeFieldRequest request);

    [Patch("/service_intake_fields")]
    Task SetFieldOrderAsync([AliasAs("id")] string idEq, [Body] SetIntakeFieldOrderRequest request);

    [Delete("/service_intake_fields")]
    Task DeleteFieldAsync([AliasAs("id")] string idEq);
}
