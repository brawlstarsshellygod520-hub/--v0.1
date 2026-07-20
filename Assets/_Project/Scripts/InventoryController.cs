using UnityEngine;

public sealed class InventoryController : MonoBehaviour
{
    [SerializeField] float pickupDistance = 2.5f;
    readonly WorldItem[] slots = new WorldItem[3];
    Camera viewCamera;
    int selected;
    string inspectedName;
    string inspectedDescription;
    GUIStyle boxStyle, titleStyle, slotStyle;

    void Start() => viewCamera = Camera.main;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selected = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selected = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selected = 2;
        if (Input.GetKeyDown(KeyCode.JoystickButton4)) selected = (selected + 2) % 3;
        if (Input.GetKeyDown(KeyCode.JoystickButton5)) selected = (selected + 1) % 3;
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton0)) ToggleHeldItem();
        if (Input.GetMouseButtonDown(0)) InspectAtPointer();
        if (Input.GetKeyDown(KeyCode.Escape)) { inspectedName = null; inspectedDescription = null; }
    }

    void ToggleHeldItem()
    {
        if (slots[selected] != null)
        {
            WorldItem item = slots[selected];
            slots[selected] = null;
            item.transform.SetParent(null);
            item.transform.position = transform.position + transform.forward * 1.3f + Vector3.up * 0.5f;
            item.gameObject.SetActive(true);
            return;
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, pickupDistance);
        WorldItem closest = null;
        float best = float.MaxValue;
        foreach (Collider hit in nearby)
        {
            WorldItem item = hit.GetComponentInParent<WorldItem>();
            if (!item) continue;
            float d = (item.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; closest = item; }
        }
        if (closest)
        {
            slots[selected] = closest;
            closest.transform.SetParent(transform);
            closest.gameObject.SetActive(false);
        }
    }

    void InspectAtPointer()
    {
        if (!viewCamera) return;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 20f)) return;
        WorldItem item = hit.collider.GetComponentInParent<WorldItem>();
        inspectedName = item ? item.displayName : hit.collider.gameObject.name;
        inspectedDescription = item ? item.description : "場景物件";
    }

    void EnsureStyles()
    {
        boxStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        slotStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
    }

    void OnGUI()
    {
        EnsureStyles();
        float total = 330f, y = Screen.height - 92f;
        for (int i = 0; i < 3; i++)
        {
            Rect r = new(Screen.width * 0.5f - total * 0.5f + i * 110f, y, 100f, 70f);
            GUI.color = i == selected ? new Color(1f, .8f, .25f) : Color.white;
            GUI.Box(r, $"{i + 1}\n{(slots[i] ? slots[i].displayName : "空")}", slotStyle);
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(15, 12, 700, 30), "WASD/左搖桿 移動｜Shift 跑｜Ctrl 蹲｜F/手把 A 撿放｜右鍵/右搖桿 視角｜左鍵 檢視", GUI.skin.label);
        if (!string.IsNullOrEmpty(inspectedName))
        {
            Rect panel = new(Screen.width * .5f - 210f, 55f, 420f, 115f);
            GUI.Box(panel, "", boxStyle);
            GUI.Label(new Rect(panel.x + 15, panel.y + 10, panel.width - 30, 32), inspectedName, titleStyle);
            GUI.Label(new Rect(panel.x + 20, panel.y + 49, panel.width - 40, 55), inspectedDescription, boxStyle);
        }
    }
}
