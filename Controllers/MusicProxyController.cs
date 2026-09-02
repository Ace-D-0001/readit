using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Read_It.Controllers;

/// <summary>
/// Music search and streaming controller supporting Full YouTube Tracks & iTunes Previews.
/// </summary>
[Route("api/music")]
public class MusicProxyController : Controller
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly List<object> CuratedYouTubeTracks = new()
    {
        new
        {
            title = "Lofi Hip Hop Radio — Beats to Relax/Study to",
            artist = "Lofi Girl",
            cover = "https://i.ytimg.com/vi/jfKfPfyJRdk/hqdefault.jpg",
            youtubeId = "jfKfPfyJRdk",
            duration = 3600,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        },
        new
        {
            title = "Chillhop Radio — Jazzy & Lofi Beats",
            artist = "Chillhop Music",
            cover = "https://i.ytimg.com/vi/5yx6BWlEVcY/hqdefault.jpg",
            youtubeId = "5yx6BWlEVcY",
            duration = 3600,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        },
        new
        {
            title = "Studio Ghibli Piano & Lofi Collection",
            artist = "Ghibli Relaxing Music",
            cover = "https://i.ytimg.com/vi/TURbeWK2wwg/hqdefault.jpg",
            youtubeId = "TURbeWK2wwg",
            duration = 7200,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        },
        new
        {
            title = "Synthwave Radio — Cyberpunk Chill Beats",
            artist = "Lofi & Synth",
            cover = "https://i.ytimg.com/vi/4xDzrJKXOOY/hqdefault.jpg",
            youtubeId = "4xDzrJKXOOY",
            duration = 3600,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        },
        new
        {
            title = "Zelda & Chill — Relaxing Gaming Lofi",
            artist = "Mikel / GameChops",
            cover = "https://i.ytimg.com/vi/GdzrrWA8e7A/hqdefault.jpg",
            youtubeId = "GdzrrWA8e7A",
            duration = 2460,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        },
        new
        {
            title = "Coffee Shop Ambience & Rainy Lofi Beats",
            artist = "Coffee Lofi",
            cover = "https://i.ytimg.com/vi/lTRiuFIWV54/hqdefault.jpg",
            youtubeId = "lTRiuFIWV54",
            duration = 3600,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        }
    };

    /// <summary>
    /// Initial full study tracks (YouTube full tracks + Lofi)
    /// GET /api/music/lofi-full
    /// </summary>
    [HttpGet("lofi-full")]
    public IActionResult GetInitialTracks()
    {
        return Json(CuratedYouTubeTracks);
    }

    /// <summary>
    /// Unified Search for YouTube Songs, URLs, and iTunes Previews.
    /// GET /api/music/search?q=...
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required" });

        var results = new List<object>();

        // 1. If user pasted a direct YouTube URL or ID
        var ytMatch = Regex.Match(q, @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w-]{11})");
        if (ytMatch.Success)
        {
            var videoId = ytMatch.Groups[1].Value;
            var ytTrack = await ResolveYouTubeVideo(videoId);
            if (ytTrack != null)
            {
                results.Add(ytTrack);
                return Json(results);
            }
        }

        // 2. Check matches from Curated YouTube Tracks
        var qLower = q.ToLower();
        var curatedMatches = CuratedYouTubeTracks
            .Where(t =>
            {
                var str = JsonSerializer.Serialize(t).ToLower();
                return str.Contains(qLower);
            })
            .ToList();

        results.AddRange(curatedMatches);

        // 3. Search iTunes
        var itunesResults = await FetchITunesTracks(q);
        results.AddRange(itunesResults);

        return Json(results);
    }

    /// <summary>
    /// Backward-compatible iTunes endpoint
    /// GET /api/music/itunes?q=...
    /// </summary>
    [HttpGet("itunes")]
    public Task<IActionResult> SearchITunes([FromQuery] string q)
    {
        return Search(q);
    }

    private async Task<object?> ResolveYouTubeVideo(string videoId)
    {
        try
        {
            var oembedUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";
            var res = await _http.GetAsync(oembedUrl);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() : "YouTube Track";
                var author = doc.RootElement.TryGetProperty("author_name", out var a) ? a.GetString() : "YouTube";
                var thumb = doc.RootElement.TryGetProperty("thumbnail_url", out var th) ? th.GetString() : $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

                return new
                {
                    title = title,
                    artist = author,
                    cover = thumb,
                    youtubeId = videoId,
                    duration = 0,
                    source = "Full Song (YouTube)",
                    badgeClass = "sp-badge-yt"
                };
            }
        }
        catch { }

        return new
        {
            title = $"YouTube Track ({videoId})",
            artist = "YouTube",
            cover = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
            youtubeId = videoId,
            duration = 0,
            source = "Full Song (YouTube)",
            badgeClass = "sp-badge-yt"
        };
    }

    private async Task<List<object>> FetchITunesTracks(string q)
    {
        var list = new List<object>();
        try
        {
            var url = $"https://itunes.apple.com/search?term={Uri.EscapeDataString(q)}&entity=song&limit=15";
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) return list;

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("results", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var trackName = item.TryGetProperty("trackName", out var tn) ? tn.GetString() : null;
                    var artistName = item.TryGetProperty("artistName", out var an) ? an.GetString() : "Artist";
                    var previewUrl = item.TryGetProperty("previewUrl", out var pu) ? pu.GetString() : null;
                    var artworkUrl = item.TryGetProperty("artworkUrl100", out var au) ? au.GetString() : null;

                    if (artworkUrl != null)
                        artworkUrl = artworkUrl.Replace("100x100bb", "300x300bb");

                    if (trackName != null && previewUrl != null)
                    {
                        list.Add(new
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
        }
        catch { }
        return list;
    }
}
