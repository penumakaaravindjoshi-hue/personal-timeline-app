using personal_timeline_backend.Models;

namespace personal_timeline_backend.Services;

public interface IThirdPartyApiService
{
    Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(int userId, string apiProvider);
    Task<bool> ConnectApiAsync(int userId, string apiProvider, string accessToken); // This is handled by OAuth middleware
    Task<bool> DisconnectApiAsync(int userId, string apiProvider);
    Task<IEnumerable<TimelineEntry>> GetRecentActivityAsync(int userId, ApiConnection connection);
    Task<bool> RefreshAccessTokenAsync(ApiConnection connection);
}
