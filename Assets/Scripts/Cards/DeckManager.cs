using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeckManager : MonoBehaviour
{
    public Card[] AllCards; //max 60
    private int[] randomIndices;
    private Card[] InDeck;
    private string line;

    [SerializeField] private bool tryRead = false;
    [SerializeField] private InputAction press;
    

    public static DeckManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        press.Enable();
        press.performed += _ => {
            Debug.Log("Deck Pressed");
        };


        InDeck = new Card[AllCards.Length];
        for (int i = 0; i < AllCards.Length; i++)
        {
            InDeck[i] = AllCards[i];
        }
        ShuffleDeck();
    }

    public void ShuffleDeck()
    {
        int deckSize = InDeck.Length;
        randomIndices = new int[deckSize];
        for (int i = 0; i < deckSize; i++)
        {
            randomIndices[i] = i;
        }
        for (int i = 0; i < deckSize; i++)
        {
            int temp = randomIndices[i];
            int randomIndex = UnityEngine.Random.Range(i, deckSize);
            randomIndices[i] = randomIndices[randomIndex];
            randomIndices[randomIndex] = temp;
        }
        Card[] shuffledDeck = new Card[deckSize];
        for (int i = 0; i < deckSize; i++)
        {
            shuffledDeck[i] = InDeck[randomIndices[i]];
        }
        InDeck = shuffledDeck;
    }

    private void Update() {
        if (tryRead){
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader("C:\\Users\\SeongJoon\\Desktop\\a.txt");
                //Read the first line of text
                line = sr.ReadLine();
                //Continue to read until you reach end of file
                while (line != null)
                {
                    //write the line to console window
                    Debug.Log(line);
                    //Read the next line
                    line = sr.ReadLine();
                }
                //close the file
                sr.Close();
            }
            catch(Exception e)
            {
                Debug.Log("Exception: " + e.Message);
            }
            finally
            {
                Debug.Log("Executing finally block.");
            }
            tryRead = false;
        }
    }
}
