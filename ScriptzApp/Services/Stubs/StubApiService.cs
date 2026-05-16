using ScriptzApp.Services.Api;

namespace ScriptzApp.Services.Stubs;

/// <summary>
/// Wraps StubScriptzApi behind IApiService so ViewModels resolve identically
/// regardless of whether the real Refit client or the stub is registered.
/// </summary>
public class StubApiService : IApiService
{
    public IScriptzApi Api { get; }

    public StubApiService()
    {
        Api = new StubScriptzApi();
    }
}
