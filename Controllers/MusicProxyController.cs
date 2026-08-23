using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Read_It.Controllers;

/// <summary>
/// Server-side proxy for music search and audio streaming.
/// Provides iTunes search + YouTube proxy streaming with high reliability.
/// </summary>
[Route("api/music")]
public class MusicProxyController : Controller
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly string[] InvidiousInstances = new[]
    {
        "https://inv.tux.pizza",
        "https://invidious.fdn.fr",
        "https://yewtu.be",
        "https://vid.puffyan.us",
        "https://invidious.privacyredirect.com"
    };

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

                    // Upgrade artwork to 300x300 for high-res crisp covers
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
        catch (Exception ex)
        {
            return Json(new object[0]);
        }
    }

    /// <summary>
    /// Search YouTube via Invidious instances.
    /// GET /api/music/search?q=coldplay
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required" });

        var results = new List<object>();

        foreach (var instance in InvidiousInstances)
        {
            try
            {
                var url = $"{instance}/api/v1/search?q={Uri.EscapeDataString(q + " music")}&type=video&sort_by=relevance";
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode) continue;

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;

                var count = 0;
                foreach (var v in doc.RootElement.EnumerateArray())
                {
                    if (count >= 10) break;

                    var videoId = v.TryGetProperty("videoId", out var vid) ? vid.GetString() : null;
                    var title = v.TryGetProperty("title", out var t) ? t.GetString() : "Unknown";
                    var author = v.TryGetProperty("author", out var a) ? a.GetString() : "Unknown";
                    var lengthSeconds = v.TryGetProperty("lengthSeconds", out var ls) ? ls.GetInt32() : 0;

                    string? thumbnail = null;
                    if (v.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.GetArrayLength() > 0)
                    {
                        foreach (var th in thumbs.EnumerateArray())
                        {
                            if (th.TryGetProperty("quality", out var qual) && qual.GetString() == "medium")
                            {
                                thumbnail = th.TryGetProperty("url", out var tUrl) ? tUrl.GetString() : null;
                                break;
                            }
                        }
                        if (thumbnail == null && thumbs.GetArrayLength() > 0)
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
                            source = "YouTube",
                            badgeClass = "sp-badge-yt"
                        });
                        count++;
                    }
                }

                if (results.Count > 0) break;
            }
            catch
            {
                continue;
            }
        }

        return Json(results);
    }

    /// <summary>
    /// Stream proxy for YouTube audio tracks
    /// </summary>
    [HttpGet("stream/{videoId}")]
    public async Task<IActionResult> Stream(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
            return BadRequest("Video ID is required");

        foreach (var instance in InvidiousInstances)
        {
            try
            {
                var url = $"{instance}/api/v1/videos/{videoId}";
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode) continue;

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var video = doc.RootElement;

                string? audioUrl = null;

                if (video.TryGetProperty("adaptiveFormats", out var adaptive))
                {
                    var bestBitrate = -1;
                    foreach (var fmt in adaptive.EnumerateArray())
                    {
                        var type = fmt.TryGetProperty("type", out var tp) ? tp.GetString() : "";
                        if (type != null && type.StartsWith("audio/"))
                        {
                            var fmtUrl = fmt.TryGetProperty("url", out var u) ? u.GetString() : null;
                            var bitrate = fmt.TryGetProperty("bitrate", out var br) ? br.GetInt32() : 0;
                            if (fmtUrl != null && bitrate > bestBitrate)
                            {
                                bestBitrate = bitrate;
                                audioUrl = fmtUrl;
                            }
                        }
                    }
                }

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
                    return await ProxyAudioStream(audioUrl);
                }
            }
            catch
            {
                continue;
            }
        }

        return NotFound(new { error = "Could not load audio stream" });
    }

    private async Task<IActionResult> ProxyAudioStream(string audioUrl)
    {
        var audioReq = new HttpRequestMessage(HttpMethod.Get, audioUrl);

        if (Request.Headers.TryGetValue("Range", out var rangeValues))
            audioReq.Headers.TryAddWithoutValidation("Range", rangeValues.ToString());

        var audioRes = await _http.SendAsync(audioReq, HttpCompletionOption.ResponseHeadersRead);
        if (!audioRes.IsSuccessStatusCode)
            return StatusCode((int)audioRes.StatusCode);

        Response.StatusCode = (int)audioRes.StatusCode;
        Response.ContentType = audioRes.Content.Headers.ContentType?.MediaType ?? "audio/webm";

        if (audioRes.Content.Headers.ContentLength.HasValue)
            Response.ContentLength = audioRes.Content.Headers.ContentLength;

        if (audioRes.Content.Headers.ContentRange != null)
            Response.Headers["Content-Range"] = audioRes.Content.Headers.ContentRange.ToString();

        await using var upstream = await audioRes.Content.ReadAsStreamAsync();
        await upstream.CopyToAsync(Response.Body, HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);

        return new EmptyResult();
    }
}
