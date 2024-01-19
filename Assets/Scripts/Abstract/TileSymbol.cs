using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract class TileSymbol : MonoBehaviour {

    public enum Occupant { UnSeen, UnUsed, Used, OuterWall, Wall, Door, Player, NPC, Key, Consumable, Weapon, Treasure };
    protected Dictionary<Occupant, char> tileSymbols = new Dictionary<Occupant, char>();

    protected virtual void Awake() {
        FillSymbolDictionary();
    }

    protected void FillSymbolDictionary() {
        tileSymbols.Add(Occupant.UnSeen, '?');
        tileSymbols.Add(Occupant.UnUsed, '=');
        tileSymbols.Add(Occupant.Used, '.');
        tileSymbols.Add(Occupant.OuterWall, '¤');
        tileSymbols.Add(Occupant.Wall, '#');
        tileSymbols.Add(Occupant.Door, 'D');
        tileSymbols.Add(Occupant.Player, 'P');
        tileSymbols.Add(Occupant.NPC, 'N');
        tileSymbols.Add(Occupant.Key, 'K');
        tileSymbols.Add(Occupant.Consumable, 'C');
        tileSymbols.Add(Occupant.Weapon, 'W');
        tileSymbols.Add(Occupant.Treasure, 'T');
    }

    protected char Symbol(Occupant occupant) {
        char foundChar;
        switch (occupant) {
            case Occupant.UnSeen:
                foundChar = tileSymbols[Occupant.UnSeen];
                break;
            case Occupant.UnUsed:
                foundChar = tileSymbols[Occupant.UnUsed];
                break;
            case Occupant.Used:
                foundChar = tileSymbols[Occupant.Used];
                break;
            case Occupant.OuterWall:
                foundChar = tileSymbols[Occupant.OuterWall];
                break;
            case Occupant.Wall:
                foundChar = tileSymbols[Occupant.Wall];
                break;
            case Occupant.Door:
                foundChar = tileSymbols[Occupant.Door];
                break;
            case Occupant.Player:
                foundChar = tileSymbols[Occupant.Player];
                break;
            case Occupant.NPC:
                foundChar = tileSymbols[Occupant.NPC];
                break;
            case Occupant.Key:
                foundChar = tileSymbols[Occupant.Key];
                break;
            case Occupant.Consumable:
                foundChar = tileSymbols[Occupant.Consumable];
                break;
            case Occupant.Weapon:
                foundChar = tileSymbols[Occupant.Weapon];
                break;
            case Occupant.Treasure:
                foundChar = tileSymbols[Occupant.Treasure];
                break;
            default:
                foundChar = '~';
                break;
        }
        return foundChar;
    }


}
