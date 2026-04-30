using System;

[System.Serializable]
public class DeckData
{
    public int formatVersion = 1;
    public string deckId;
    public string deckName;
    public int[] cardIds = new int[0];
    public long createdAtUnixTime;
    public long updatedAtUnixTime;

    public static DeckData Create(string deckName, int[] cardIds)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new DeckData
        {
            formatVersion = 1,
            deckId = Guid.NewGuid().ToString("N"),
            deckName = string.IsNullOrWhiteSpace(deckName) ? "New Deck" : deckName.Trim(),
            cardIds = cardIds ?? new int[0],
            createdAtUnixTime = now,
            updatedAtUnixTime = now
        };
    }

    public void EnsureMetadata()
    {
        if (formatVersion <= 0) formatVersion = 1;
        if (string.IsNullOrWhiteSpace(deckId)) deckId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(deckName)) deckName = "New Deck";
        if (cardIds == null) cardIds = new int[0];

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (createdAtUnixTime <= 0) createdAtUnixTime = now;
        if (updatedAtUnixTime <= 0) updatedAtUnixTime = createdAtUnixTime;
    }

    public void Touch()
    {
        EnsureMetadata();
        updatedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
