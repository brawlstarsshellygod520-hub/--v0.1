# 角色 v0.2 驗證摘要

2026-10-05，Unity 6000.5.0b9，Built-in，單人 Editor；沒有 Player build／多人結果。

- `character-animation-results.txt`：31 PASS，batch exit 0。
- `residential-results.txt`：40 PASS，本輪在新角色／動畫接線後重跑，batch exit 0。
- `character-gameplay-preservation.txt`：只換 Visual 之前／之後，原玩法 MonoBehaviour／Collider JSON 一致。
- `character-art-import.txt`：實際 Unity 面數與 skinned 動畫 bounds；bounds 不是作者靜態身高。
- 原玩法／其他家具／ProjectSettings 57 檔 SHA256 未變，兩組 FBX／Prefab GUID 保留。
- 新 C# error／runtime Exception 為 0。舊測試 API deprecated 警告、背景授權 token 訊息不是新增玩法錯誤；完整日誌留於 Logs。

早期問題與修正：Blender 5.1 layered Action API、從目前開啟 `.blend` append 同一檔案會報錯（改臨時工作檔）、動作名稱／權重／根循環檢查、分段珠狀腿的造型修正。Unity 預覽地板自陰影與同幀多次 Camera.Render 的 skin cache 另修正於臨時攝影棚，與 Play Mode 動作／玩法無關。

不以骨架檔存在替代 runtime 證據，也不以舊角色階段的歷史 PASS 代替本輪測試。手感及「與參考圖是否符合期待」仍由使用者試玩／審圖；已保存真實 Unity PNG 與 GIF 方便直接對照。
