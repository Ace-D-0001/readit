using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Read_It.Controllers;

/// <summary>
/// Music search and preview controller using iTunes 30s previews.
/// </summary>
[Route("api/music")]
public class MusicProxyController : Controller
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// Initial study tracks (30s iTunes previews)
    /// GET /api/music/lofi-full
    /// </summary>
    [HttpGet("lofi-full")]
    public async Task<IActionResult> GetInitialTracks()
    {
        return await SearchITunes("lofi chill study");
    }

    /// <summary>
    /// Search iTunes API for 30s song previews.
    /// GET /api/music/itunes?q=coldplay
    /// </summary>
    [HttpGet("itunes")]
    public async Task<IActionResult> SearchITunes([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required" });

        try
        {
            var url = $"https://itunes.apple.com/search?term={Uri.EscapeDataString(q)}&entity=song&limit=15";
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
                return Json(new object[0]);

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = new List<object>();
            if (doc.RootElement.TryGetProperty("results", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var trackName = item.TryGetProperty("trackName", out var tn) ? tn.GetString() : null;
                    var artistName = item.TryGetProperty("artistName", out var an) ? an.GetString() : "iTunes Artist";
                    var previewUrl = item.TryGetProperty("previewUrl", out var pu) ? pu.GetString() : null;
                    var artworkUrl = item.TryGetProperty("artworkUrl100", out var au) ? au.GetString() : null;

                    if (artworkUrl != null)
                        artworkUrl = artworkUrl.Replace("100x100bb", "300x300bb");

                    if (trackName != null && previewUrl != null)
                    {
                        results.Add(new
                        {
                            title = trackName,
                            artist = artistName,
                            cover = artworkUrl ?? "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=300",
                            streamUrl = previewUrl,
                            duration = 30,
                            source = "30s iTunes Preview",
                            badgeClass = "sp-badge-itunes"
                        });
                    }
                }
            }

            return Json(results);
        }
        catch
        {
            return Json(new object[0]);
        }
    }
}
