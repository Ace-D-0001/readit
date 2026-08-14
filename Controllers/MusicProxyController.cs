using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Read_It.Controllers;

/// <summary>
/// Server-side proxy for music search and audio streaming.
/// Searches YouTube via Invidious API and proxies audio streams
/// to avoid CORS restrictions in the browser.
/// </summary>
[Route("api/music")]
public class MusicProxyController : Controller
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly string[] InvidiousInstances = new[]
    {
        "https://inv.tux.pizza",
        "https://invidious.fdn.fr",
        "https://yewtu.be",
        "https://vid.puffyan.us",
        "https://invidious.privacyredirect.com",
        "https://iv.datura.network"
    };

    /// <summary>
    /// Search for tracks across YouTube (via Invidious).
    /// GET /api/music/search?q=coldplay+yellow
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required" });

        var results = new List<object>();

        // Try each Invidious instance until one works
        foreach (var instance in InvidiousInstances)
        {
            try
            {
                var url = $"{instance}/api/v1/search?q={Uri.EscapeDataString(q + " music")}&type=video&sort_by=relevance";
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode) continue;

                var json = await res.Content.ReadAsStringAsync();
                var videos = JsonSerializer.Deserialize<JsonElement[]>(json);
                if (videos == null || videos.Length == 0) continue;

                foreach (var v in videos.Take(10))
                {
                    var videoId = v.TryGetProperty("videoId", out var vid) ? vid.GetString() : null;
                    var title = v.TryGetProperty("title", out var t) ? t.GetString() : "Unknown";
                    var author = v.TryGetProperty("author", out var a) ? a.GetString() : "Unknown";
                    var lengthSeconds = v.TryGetProperty("lengthSeconds", out var ls) ? ls.GetInt32() : 0;

                    string? thumbnail = null;
                    if (v.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.GetArrayLength() > 0)
                    {
                        // Try to find a medium quality thumbnail
                        foreach (var th in thumbs.EnumerateArray())
                        {
                            if (th.TryGetProperty("quality", out var qual) && qual.GetString() == "medium")
                            {
                                thumbnail = th.TryGetProperty("url", out var tUrl) ? tUrl.GetString() : null;
                                break;
                            }
                        }
                        // Fallback to first thumbnail
                        if (thumbnail == null)
                        {
                            var first = thumbs[0];
                            thumbnail = first.TryGetProperty("url", out var tUrl) ? tUrl.GetString() : null;
                        }
                    }

                    if (videoId != null)
                    {
                        results.Add(new
                        {
                            videoId,
                            title,
                            artist = author,
                            duration = lengthSeconds,
                            cover = thumbnail ?? $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
                            streamUrl = $"/api/music/stream/{videoId}",
                            source = "YouTube"
                        });
                    }
                }

                break; // Success, stop trying other instances
            }
            catch
            {
                continue; // Try next instance
            }
        }

        return Json(results);
    }

    /// <summary>
    /// Proxy an audio stream for a YouTube video.
    /// GET /api/music/stream/{videoId}
    /// </summary>
    [HttpGet("stream/{videoId}")]
    public async Task<IActionResult> Stream(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
            return BadRequest("Video ID is required");

        // Try each Invidious instance to get audio URL
        foreach (var instance in InvidiousInstances)
        {
            try
            {
                var url = $"{instance}/api/v1/videos/{videoId}";
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode) continue;

                var json = await res.Content.ReadAsStringAsync();
                var video = JsonSerializer.Deserialize<JsonElement>(json);

                string? audioUrl = null;

                // Try adaptive formats first (audio-only, best quality)
                if (video.TryGetProperty("adaptiveFormats", out var adaptive))
                {
                    var audioFormats = new List<(string url, int bitrate)>();
                    foreach (var fmt in adaptive.EnumerateArray())
                    {
                        var type = fmt.TryGetProperty("type", out var tp) ? tp.GetString() : "";
                        if (type != null && type.StartsWith("audio/"))
                        {
                            var fmtUrl = fmt.TryGetProperty("url", out var u) ? u.GetString() : null;
                            var bitrate = fmt.TryGetProperty("bitrate", out var br) ? br.GetInt32() : 0;
                            if (fmtUrl != null)
                                audioFormats.Add((fmtUrl, bitrate));
                        }
                    }

                    if (audioFormats.Count > 0)
                    {
                        audioUrl = audioFormats.OrderByDescending(f => f.bitrate).First().url;
                    }
                }

                // Fallback to format streams
                if (audioUrl == null && video.TryGetProperty("formatStreams", out var streams))
                {
                    foreach (var fmt in streams.EnumerateArray())
                    {
                        var fmtUrl = fmt.TryGetProperty("url", out var u) ? u.GetString() : null;
                        if (fmtUrl != null)
                        {
                            audioUrl = fmtUrl;
                            break;
                        }
                    }
                }

                if (audioUrl != null)
                {
                    // Proxy the audio stream
                    var audioRes = await _http.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead);
                    if (audioRes.IsSuccessStatusCode)
                    {
                        var contentType = audioRes.Content.Headers.ContentType?.ToString() ?? "audio/webm";
                        var stream = await audioRes.Content.ReadAsStreamAsync();
                        return File(stream, contentType);
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return NotFound(new { error = "Could not load audio stream" });
    }
}
