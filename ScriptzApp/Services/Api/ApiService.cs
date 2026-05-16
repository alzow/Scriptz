namespace ScriptzApp.Services.Api;

public class ApiService : IApiService
{
    public IScriptzApi Api { get; }

    public ApiService(IScriptzApi api)
    {
        Api = api;
    }
}
