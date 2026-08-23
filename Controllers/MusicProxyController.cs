using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Read_It.Controllers;

/// <summary>
/// Server-side music search controller for iTunes + Jamendo APIs.
/// Provides instant, legal, 100% reliable song search and previews.
/// </summary>
[Route("api/music")]
public class MusicProxyController : Controller
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string JamendoClientId = "5b6e5f02";

    /// <summary>
    /// Search official iTunes database for instant high-quality audio previews + high-res artwork.
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
                    var artistName = item.TryGetProperty("artistName", out var an) ? an.GetString() : "Unknown Artist";
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
                            source = "iTunes",
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

    /// <summary>
    /// Search Jamendo API for full-length, legally licensed MP3 tracks across all genres.
    /// GET /api/music/jamendo?q=lofi
    /// </summary>
    [HttpGet("jamendo")]
    public async Task<IActionResult> SearchJamendo([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required" });

        try
        {
            var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={JamendoClientId}&format=json&limit=15&namesearch={Uri.EscapeDataString(q)}&include=musicinfo&audioformat=mp32";
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
                    var trackName = item.TryGetProperty("name", out var tn) ? tn.GetString() : null;
                    var artistName = item.TryGetProperty("artist_name", out var an) ? an.GetString() : "Jamendo Artist";
                    var audioUrl = item.TryGetProperty("audio", out var au) ? au.GetString() : null;
                    var albumImage = item.TryGetProperty("album_image", out var ai) ? ai.GetString() : null;
                    var duration = item.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0;

                    if (trackName != null && audioUrl != null)
                    {
                        results.Add(new
                        {
                            title = trackName,
                            artist = artistName,
                            cover = albumImage ?? "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=300",
                            streamUrl = audioUrl,
                            duration = duration,
                            source = "Jamendo",
                            badgeClass = "sp-badge-jamendo"
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
