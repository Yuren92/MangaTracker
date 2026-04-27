using System.Text.Json.Serialization;

namespace MangaTracker.Infrastructure.ExternalServices.MyAnimeList;

internal sealed class MalMangaResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("main_picture")]
    public MalMainPicture? MainPicture { get; set; }

    [JsonPropertyName("synopsis")]
    public string? Synopsis { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("num_volumes")]
    public int? NumVolumes { get; set; }

    [JsonPropertyName("num_chapters")]
    public int? NumChapters { get; set; }

    [JsonPropertyName("recommendations")]
    public List<MalMangaRecommendationResponse> Recommendations { get; set; } = [];
}



internal sealed class MalMainPicture
{
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

internal sealed class MalMangaSearchResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public List<MalMangaSearchItem> Data { get; set; } = [];
}

internal sealed class MalMangaSearchItem
{
    [System.Text.Json.Serialization.JsonPropertyName("node")]
    public MalMangaSearchNode Node { get; set; } = new();
}

internal sealed class MalMangaSearchNode
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("main_picture")]
    public MalMainPicture? MainPicture { get; set; }
}

internal sealed class MalMangaRecommendationResponse
{
    [JsonPropertyName("node")]
    public MalMangaSearchNode Node { get; set; } = new();

    [JsonPropertyName("num_recommendations")]
    public int NumRecommendations { get; set; }
}
