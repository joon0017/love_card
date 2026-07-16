using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class DeckStorage
{
    public const string ActiveDeckPlayerPrefsKey = "loveca.activeDeckId";
    public const string DeckFileExtension = ".deck";

    private const string DeckDirectoryName = "Decks";
    private const string LegacyDeckFileName = "deck.dat";
    private const string EncryptionKey = "1234567890123456";

    public static string DeckDirectoryPath => Path.Combine(Application.persistentDataPath, DeckDirectoryName);
    public static string LegacyDeckFilePath => Path.Combine(Application.persistentDataPath, LegacyDeckFileName);

    public static DeckData CreateDeck(string deckName, IEnumerable<int> cardIds = null)
    {
        return DeckData.Create(deckName, cardIds != null ? cardIds.ToArray() : new int[0]);
    }

    public static string EncodeDeck(DeckData deckData)
    {
        if (deckData == null) throw new ArgumentNullException(nameof(deckData));

        deckData.EnsureMetadata();
        string json = JsonUtility.ToJson(deckData);
        return CryptoUtil.Encrypt(json, EncryptionKey);
    }

    public static bool TryDecodeDeck(string encodedDeckData, out DeckData deckData, out string error)
    {
        deckData = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(encodedDeckData))
        {
            error = "Encoded deck data is empty.";
            return false;
        }

        try
        {
            string json = CryptoUtil.Decrypt(encodedDeckData.Trim(), EncryptionKey);
            deckData = JsonUtility.FromJson<DeckData>(json);

            if (deckData == null)
            {
                error = "Decoded deck data was empty.";
                return false;
            }

            deckData.EnsureMetadata();
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static string SaveDeck(DeckData deckData)
    {
        if (deckData == null) throw new ArgumentNullException(nameof(deckData));

        deckData.Touch();
        Directory.CreateDirectory(DeckDirectoryPath);

        string filePath = GetDeckFilePath(deckData.deckId);
        File.WriteAllText(filePath, EncodeDeck(deckData));
        return filePath;
    }

    public static bool TryLoadDeck(string deckId, out DeckData deckData, out string error)
    {
        deckData = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(deckId))
        {
            error = "Deck id is empty.";
            return false;
        }

        string filePath = GetDeckFilePath(deckId);
        if (!File.Exists(filePath))
        {
            error = $"Deck file was not found: {filePath}";
            return false;
        }

        return TryDecodeFile(filePath, out deckData, out error);
    }

    public static DeckData[] LoadAllDecks()
    {
        if (!Directory.Exists(DeckDirectoryPath)) return new DeckData[0];

        List<DeckData> decks = new List<DeckData>();
        foreach (string filePath in Directory.GetFiles(DeckDirectoryPath, "*" + DeckFileExtension))
        {
            if (TryDecodeFile(filePath, out DeckData deckData, out string error))
            {
                decks.Add(deckData);
            }
            else
            {
                Debug.LogWarning($"Could not load deck file '{filePath}': {error}");
            }
        }

        return decks
            .OrderByDescending(deckData => deckData.updatedAtUnixTime)
            .ThenBy(deckData => deckData.deckName)
            .ToArray();
    }

    public static bool DeleteDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId)) return false;

        string filePath = GetDeckFilePath(deckId);
        if (!File.Exists(filePath)) return false;

        File.Delete(filePath);
        if (GetActiveDeckId() == deckId)
        {
            ClearActiveDeck();
        }

        return true;
    }

    public static bool TryImportDeck(string encodedDeckData, out DeckData deckData, out string error, bool saveToLibrary = true, string deckNameOverride = null)
    {
        if (!TryDecodeDeck(encodedDeckData, out deckData, out error)) return false;

        deckData.deckId = Guid.NewGuid().ToString("N");
        if (!string.IsNullOrWhiteSpace(deckNameOverride))
        {
            deckData.deckName = deckNameOverride.Trim();
        }

        deckData.createdAtUnixTime = 0;
        deckData.Touch();

        if (saveToLibrary)
        {
            SaveDeck(deckData);
        }

        return true;
    }

    public static bool TryLoadActiveDeck(out DeckData deckData, out string error)
    {
        string activeDeckId = GetActiveDeckId();
        if (string.IsNullOrWhiteSpace(activeDeckId))
        {
            deckData = null;
            error = "No active deck has been selected.";
            return false;
        }

        return TryLoadDeck(activeDeckId, out deckData, out error);
    }

    public static void SetActiveDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            ClearActiveDeck();
            return;
        }

        PlayerPrefs.SetString(ActiveDeckPlayerPrefsKey, deckId);
        PlayerPrefs.Save();
    }

    public static string GetActiveDeckId()
    {
        return PlayerPrefs.GetString(ActiveDeckPlayerPrefsKey, string.Empty);
    }

    public static void ClearActiveDeck()
    {
        PlayerPrefs.DeleteKey(ActiveDeckPlayerPrefsKey);
        PlayerPrefs.Save();
    }

    public static bool TryLoadLegacyDeck(out DeckData deckData, out string error)
    {
        if (!File.Exists(LegacyDeckFilePath))
        {
            deckData = null;
            error = "Legacy deck.dat was not found.";
            return false;
        }

        bool loaded = TryDecodeFile(LegacyDeckFilePath, out deckData, out error);
        if (loaded)
        {
            deckData.deckName = "Imported Legacy Deck";
        }

        return loaded;
    }

    private static bool TryDecodeFile(string filePath, out DeckData deckData, out string error)
    {
        try
        {
            string encodedDeckData = File.ReadAllText(filePath);
            return TryDecodeDeck(encodedDeckData, out deckData, out error);
        }
        catch (Exception exception)
        {
            deckData = null;
            error = exception.Message;
            return false;
        }
    }

    private static string GetDeckFilePath(string deckId)
    {
        return Path.Combine(DeckDirectoryPath, SanitizeDeckId(deckId) + DeckFileExtension);
    }

    private static string SanitizeDeckId(string deckId)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitizedDeckId = new string(deckId.Where(character => !invalidChars.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitizedDeckId) ? Guid.NewGuid().ToString("N") : sanitizedDeckId;
    }
}
 