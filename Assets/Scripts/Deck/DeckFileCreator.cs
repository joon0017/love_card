using System.IO;
using UnityEngine;

public class DeckFileCreator : MonoBehaviour
{
    public Card[] AllCards;

    private const string encryptionKey = "1234567890123456";
    void Awake(){
        // Example deck
        DeckData deck = new DeckData
        {
            cardIds = new int[]
            {
                0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3 ,4, 4, 4, 4, 5, 5, 5, 5
            }
        };

        string json = JsonUtility.ToJson(deck, true);
        string encrypted = CryptoUtil.Encrypt(json, encryptionKey);

        string path = Path.Combine(Application.persistentDataPath, "deck.dat");
        File.WriteAllText(path, encrypted);

        Debug.Log("Encrypted deck created at:\n" + path);
    }
}
