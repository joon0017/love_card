using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class CardImageCache
{
    private sealed class Entry { public Texture2D Texture; public Sprite Sprite; public int References; }
    private static readonly Dictionary<string, Entry> Memory = new Dictionary<string, Entry>();
    private static readonly HashSet<string> Loading = new HashSet<string>();
    private const long MaxDiskCacheBytes = 256L * 1024L * 1024L;
    private const int MaxDownloadAttempts = 3;
    public static string CacheDirectory => Path.Combine(Application.persistentDataPath, "CardImageCache");

    public static IEnumerator Acquire(string url, Action<Sprite> completed)
    {
        if (string.IsNullOrWhiteSpace(url)) { completed?.Invoke(null); yield break; }
        if (Memory.TryGetValue(url, out Entry cached)) { cached.References++; completed?.Invoke(cached.Sprite); yield break; }
        if (Loading.Contains(url))
        {
            while (Loading.Contains(url)) yield return null;
            if (Memory.TryGetValue(url, out cached)) { cached.References++; completed?.Invoke(cached.Sprite); }
            else completed?.Invoke(null);
            yield break;
        }

        Loading.Add(url);
        Directory.CreateDirectory(CacheDirectory);
        string cachePath = Path.Combine(CacheDirectory, Hash(url) + ".img");
        byte[] bytes = null;
        if (File.Exists(cachePath))
        {
            try { bytes = File.ReadAllBytes(cachePath); File.SetLastAccessTimeUtc(cachePath, DateTime.UtcNow); }
            catch (Exception error) { Debug.LogWarning($"Could not read cached card image: {error.Message}"); }
        }

        if (bytes == null || bytes.Length == 0)
        {
            string lastError = "Unknown download error";
            for (int attempt = 1; attempt <= MaxDownloadAttempts; attempt++)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = 30;
                    request.SetRequestHeader("Accept", "image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
                    request.SetRequestHeader("Referer", "https://llofficial-cardgame.com/cardlist/");
                    request.SetRequestHeader("User-Agent", "Mozilla/5.0 (compatible; LovecaGame/1.0)");
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        bytes = request.downloadHandler.data;
                        break;
                    }
                    lastError = $"{request.responseCode} {request.error}";
                }

                if (attempt < MaxDownloadAttempts)
                    yield return new WaitForSecondsRealtime(attempt);
            }

            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning($"Could not download card image '{url}' after {MaxDownloadAttempts} attempts: {lastError}");
                Loading.Remove(url); completed?.Invoke(null); yield break;
            }

            try { File.WriteAllBytes(cachePath, bytes); TrimDiskCache(cachePath); }
            catch (Exception error) { Debug.LogWarning($"Could not cache card image: {error.Message}"); }
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(texture, bytes, true))
        {
            UnityEngine.Object.Destroy(texture);
            try { File.Delete(cachePath); } catch { }
            Loading.Remove(url); completed?.Invoke(null); yield break;
        }
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        Memory[url] = new Entry { Texture = texture, Sprite = sprite, References = 1 };
        Loading.Remove(url);
        completed?.Invoke(sprite);
    }

    public static void Release(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Memory.TryGetValue(url, out Entry entry)) return;
        if (--entry.References > 0) return;
        UnityEngine.Object.Destroy(entry.Sprite);
        UnityEngine.Object.Destroy(entry.Texture);
        Memory.Remove(url);
    }

    public static void ClearDiskCache() { if (Directory.Exists(CacheDirectory)) Directory.Delete(CacheDirectory, true); }

    private static void TrimDiskCache(string protectedPath)
    {
        DirectoryInfo directory = new DirectoryInfo(CacheDirectory);
        FileInfo[] files = directory.GetFiles("*.img");
        long total = 0;
        foreach (FileInfo file in files) total += file.Length;
        Array.Sort(files, (left, right) => left.LastAccessTimeUtc.CompareTo(right.LastAccessTimeUtc));
        foreach (FileInfo file in files)
        {
            if (total <= MaxDiskCacheBytes) break;
            if (string.Equals(file.FullName, protectedPath, StringComparison.OrdinalIgnoreCase)) continue;
            try { total -= file.Length; file.Delete(); } catch { }
        }
    }

    private static string Hash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            StringBuilder result = new StringBuilder(hash.Length * 2);
            foreach (byte item in hash) result.Append(item.ToString("x2"));
            return result.ToString();
        }
    }
}
