using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Read_It.Controllers;

/// <summary>
/// Backend API controller providing full-length MP3 songs (Jamendo + Curated Study Tracks)
/// as well as iTunes previews.
/// </summary>
[Route("api/music")]
public class MusicProxyController : Controller
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly IConfiguration _config;

    public MusicProxyController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Initial full-length study tracks (100% full songs, 3-5 mins long)
    /// GET /api/music/lofi-full
    /// </summary>
    [HttpGet("lofi-full")]
    public async Task<IActionResult> GetInitialFullTracks()
    {
        var clientId = _config["Jamendo:ClientId"] ?? "5b6e5f02";
        try
        {
            var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={clientId}&format=json&limit=10&search=lofi+chill&include=musicinfo&audioformat=mp32";
            var res = await _http.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var results = new List<object>();
                if (doc.RootElement.TryGetProperty("results", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var trackName = item.TryGetProperty("name", out var tn) ? tn.GetString() : null;
                        var artistName = item.TryGetProperty("artist_name", out var an) ? an.GetString() : "Lofi Study Artist";
                        var audioUrl = item.TryGetProperty("audio", out var au) ? au.GetString() : null;
                        var albumImage = item.TryGetProperty("album_image", out var ai) ? ai.GetString() : null;
                        var duration = item.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0;

                        if (trackName != null && audioUrl != null)
                        {
                            results.Add(new
                            {
                                title = trackName,
                                artist = artistName,
                                cover = albumImage ?? "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?w=300",
                                streamUrl = audioUrl,
                                duration = duration,
                                source = "Full Song",
                                badgeClass = "sp-badge-jamendo"
                            });
                        }
                    }
                }
                if (results.Count > 0) return Json(results);
            }
        }
        catch { }

        // Fallback curated full tracks if offline
        return Json(new object[]
        {
            new {
                title = "Deep Focus Ambient Study",
                artist = "Chill Study Beats",
                cover = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=300",
                streamUrl = "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/no_curator/Tours/Enthusiast/Tours_-_01_-_Enthusiast.mp3",
                duration = 184,
                source = "Full Song",
                badgeClass = "sp-badge-jamendo"
            },
            new {
                title = "Peaceful Midnight Coffee",
                artist = "Lofi Lounge",
                cover = "https://images.unsplash.com/photo-1501386761578-eac5c94b800a?w=300",
                streamUrl = "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/ccCommunity/Kai_Engel/Shatter_Me/Kai_Engel_-_04_-_Sentinel.mp3",
                duration = 210,
                source = "Full Song",
                badgeClass = "sp-badge-jamendo"
            }
        });
    }

    /// <summary>
    /// Search Jamendo API for full-length Creative Commons MP3 tracks.
    /// GET /api/music/jamendo?q=lofi
    /// </summary>
    [HttpGet("jamendo")]
    public async Task<IActionResult> SearchJamendo([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required" });

        var clientId = _config["Jamendo:ClientId"] ?? "5b6e5f02";

        try
        {
            var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={clientId}&format=json&limit=15&search={Uri.EscapeDataString(q)}&include=musicinfo&audioformat=mp32";
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
                            source = "Full Song",
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

    /// <summary>
    /// Search iTunes database for 30-second audio previews + high-res artwork.
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
                            source = "30s Preview",
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
