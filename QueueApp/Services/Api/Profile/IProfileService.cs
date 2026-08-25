namespace QueueApp.Services.Api.Profile;

public interface IProfileService
{
    Task<string> GetMyDisplayNameAsync(Guid userId);
}
