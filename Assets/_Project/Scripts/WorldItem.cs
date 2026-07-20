using UnityEngine;

public sealed class WorldItem : MonoBehaviour
{
    public string displayName = "測試物品";
    [TextArea] public string description = "可用 F 撿起並放入物品欄。";
}
