using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace DiscordBot.Services;

public class YouTubeApiService
{
    private static YouTubeApiService? yt;
    private readonly YouTubeService service;

    private YouTubeApiService()
    {
        yt = this;
        service = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = Secrets.GoogleApiKey
        });
    }

    /// <summary>
    ///     Gets the <see cref="YouTubeApiService" /> instance
    /// </summary>
    /// <returns></returns>
    public static YouTubeApiService Get()
    {
        return yt ?? new YouTubeApiService();
    }

    /// <summary>
    ///     Searches youtube for a given search term
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>A list of search results. Empty if none where found</returns>
    public async Task<List<SearchResult>> Search(string searchTerm)
    {
        var searchListRequest = service.Search.List("snippet");
        searchListRequest.Q = searchTerm;
        searchListRequest.Type = "video";
        searchListRequest.VideoCategoryId = "10";
        // searchListRequest.VideoDuration = SearchResource.ListRequest.VideoDurationEnum.Short__;
        searchListRequest.MaxResults = 15;

        var response = (await searchListRequest.ExecuteAsync())?.Items ?? new List<SearchResult>();

        List<SearchResult> results = response.ToList()
            .FindAll(item => item.Snippet.LiveBroadcastContent == "none");

        return results;
    }
}