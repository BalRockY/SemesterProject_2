using UnityEngine;
[CreateAssetMenu(fileName = "Item File", menuName = "New Item")]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    [TextArea(10, 15)]
    public string itemDescrion;
}
