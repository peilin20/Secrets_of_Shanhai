using System.Collections.Generic;
using UnityEngine;

// What one player picked in the character creation scene.
[System.Serializable]
public class PlayerSetup
{
    public string name;
    public int age = -1;          // -1: not entered.
    public Species species;
    public Sprite portrait;
    public Element primaryElement;
    public List<Element> tokens = new List<Element>();
    public BackgroundData background;
    public ClassData characterClass;
    public List<ItemData> items = new List<ItemData>();
    public int equippedSlot = -1;
    public List<InjuryData> injuries = new List<InjuryData>();
}

// Carries the players from the character creation scene into the game scene.
public static class Party
{
    public static readonly List<PlayerSetup> players = new List<PlayerSetup>();
    public static readonly List<ItemData> takenFromPile = new List<ItemData>();   // Cards a background took out of the item pile.

    public static void Clear()
    {
        players.Clear();
        takenFromPile.Clear();
    }
}
