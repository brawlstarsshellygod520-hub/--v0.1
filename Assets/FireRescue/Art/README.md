# 模型來源分類

- **UserProvided**：使用者提供的角色、戶型、消防斧、噴槍；包含以這些模型加工的骨架、動畫、材質與Prefab，不能標成AI原創。
- **AIGenerated**：AI撰寫Blender腳本生成的舊模型，以及Unity Primitive灰盒家具。
- **Shaders**：共用渲染程式，不是模型。

可編輯來源與原始FBX／Blend放在專案根目錄 `ArtSource/UserProvided`、`ArtSource/AIGenerated`。角色的加工版在 `UserProvided/Characters/Rigged`；正式模型替換只換Visual，保留玩法Root與Anchors。

詳細對照與來源證據見 `Docs/ASSET_CLASSIFICATION.md`。Unity資產請用Project視窗／AssetDatabase搬移，勿遺失或重建.meta GUID。
