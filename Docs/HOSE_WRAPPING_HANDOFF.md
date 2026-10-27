# 接續點：自動繞牆管線＋Unity MCP（2026-10-27）

> 2026-10-28已由新需求接續：目前FirstReview1027啟用自動繞牆＋15m（使用者確認），並改連續3D管身呈現。舊FirstRescue1026沒有覆寫。最新狀態讀 `Docs/PROTOTYPE_CONTEXT.md` 及 `Docs/FIRST_REVIEW_1027.md`，不要照下方舊步驟重跑Install或優先安裝MCP。使用者目前禁止螢幕控制；MCP連線未完成但不阻擋背景驗證。

使用者要求剩餘額度先寫紀錄，此處停止實作。**尚未交付新玩家版，不可回報整合完成。**

## 目前已做

- 使用者新規格取代舊「遇牆卡住」：管線應像實體繞牆、回頭解繞，整條繞行路徑共用5m。
- 新增 `Assets/FireRescue/Runtime/Hose/HoseWrapPath.cs`：最多24個接觸點；只從玩家上一個可達端點記錄路徑，不做全圖最短路徑跳門。收緊接點並在可行時解繞；以掃掠三角形內的牆截面防止直接把柱子內的整圈繩子拉穿。
- `HoseConnection.cs` 新增 opt-in `automaticWallWrapping`、`SetAutomaticWrapping(bool)`、`AutomaticBendCount`、`PathPointCount/GetPathPoint`；繞行長度包含接點與實際inlet的XZ距離。Runtime預設仍false，舊場景規則保留。
- `SprayWallOcclusion.cs` 新增獨立管線粗細碰撞／牆面截面快取。噴水原TryBlock不變；不依賴可替換Visual。
- `HoseView.cs` 讀同一權威折線，逐段渲染白管並保留鬆弛／碰撞保護。`FirefighterMotor.Face` 在轉身移動inlet時重套同一長度約束，位置仍由motor移動。
- 新增 `Editor/HoseWrapValidation.cs`。`BatchRun` 已在Unity背景Play Mode執行，**29項PASS**：同fixture重現舊直線卡住、繞牆後可達、每段清牆、白管存在且不穿牆、5m總路徑、持續拉不延長／不累積外推速度、回頭解繞、斷接／來源停用清理、柱子繞行與反向解圈。
- 證據 `Docs/Validation/2026-10-27-hose-wrapping/playmode.txt`、根目錄 `hose-wrap-tests.log`。**兩張fixture離線PNG目前全白，不能稱視覺驗收通過。** 要補灰地板、對比材質與相機／燈光後重新渲染；也必須看第一關實際畫面。

## MCP 真正狀態

- 使用者明確改為允許螢幕控制，並再次確認安裝 CoplayDev＋本機Python/uv。
- 已透過電腦控制開Unity／Package Manager，但彈出選單操作無法穩定，最後 `activate_window` 逾時；之後停止UI重試。不要宣稱已用畫面成功點完安裝。
- 改由 `Packages/manifest.json` 加單一固定版 `com.coplaydev.unity-mcp`，URL `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0`。
- Unity已匯入，lock hash `aa5fc638d623f56178d50329d9ad8c541a57fe66`；實際package `Library/PackageCache/com.coplaydev.unity-mcp@da7b9ae4aeeb`。`mcp-import.log` exit0，其後含套件的29項測試也通過。
- 官方uv zip下載並解壓至 `Tools/UnityMCP/bin/`，`uv --version`確認 **0.13.6**。沒有全域修改PATH，沒有執行網路安裝腳本。這些zip/bin是本機依賴，不應隨原型發佈／commit；接續時可加精準ignore，不要廣泛清理。
- 讀到 `C:/Users/user/.codex/config.toml` **本來就有** `[mcp_servers.unityMCP] url="http://127.0.0.1:8080/mcp"`，本輪沒有修改或重複新增。
- 檢查時8080沒有listener。**尚未啟動Python MCP server、尚未接Unity Bridge、尚未做tools/resources低風險probe。** 不能叫「已連線」。
- 在本機套件可讀到 `MCPForUnity.Editor.Services.MCPServiceLocator.Server` / `.Bridge`；介面有 `StartLocalHttpServer(quiet)`、`Bridge.StartAsync()`。EditorPrefs keys：`MCPForUnity.UvxPath`、`MCPForUnity.UseHttpTransport`、`MCPForUnity.HttpUrl`、`MCPForUnity.HttpTransportScope`、`MCPForUnity.AutoStartOnLoad`。務必先讀實際service再用；不要啟動會終止其他服務的功能，先查8080。只綁127.0.0.1，不開LAN／遠端權限。
- UI測試所開的Unity是本輪自啟的空白未修改Untitled，關閉前已檢查project/title無星號；現在沒有Unity或玩家程序，沒有關閉使用者先前的工作。

## 下一步，照順序

1. 完成MCP本機服務與Unity Bridge設定，讀版本／場景／Console作只讀連線證明。若現有聊天無法熱載入MCP工具，明講需重新載入聊天，不把HTTP服務就緒冒充工具已在本聊天可用。
2. 執行 `FireRescue.Editor.HoseWrapValidation.BatchEnable`：只在現有 `FirstRescue1026.unity` 啟用自動繞牆、停用但保留浴室手動guide、清空該input的hoseWindows；**此方法尚未執行**，所以目前Scene仍舊卡牆模式。檢查scene小範圍diff，不重建地圖。
3. 同步 `FirstRescue1026Setup.Install` 配方以便未來建立時採新設定，但不要重新執行Install覆寫現有場景。
4. 更新 `FirstRescue1026Validation`：原本浴室門外按F固定導管那段改成**不按F**；走到噴水站位後確認自動接觸點、5m內、無ActiveWindow，完整噴水清路／NPC逃生仍成功。否則原測試F可能會斷開水源，不能直接照跑求過。
5. 重跑29項、舊 `HosePhysicsValidation.BatchRun`（opt-in false相容性）、更新後第一人救援／重開。確認轉身inlet、門框兩角、牆邊退回不抖、thin/merged wall（真地圖）、接點上限安全停止。補可讀的實際渲染；目前還沒做螢幕玩法驗收。
6. 確認全流程後才執行 `FirstRescue1026Setup.BatchBuildLatest`、新exe D3D11 `--verify-prototype`。`PlayLatest.cmd`仍指Builds/Latest；現在該玩家版仍是上一輪，不含自動繞牆。
7. 更新 `Docs/FIRST_RESCUE_1026.md` 等操作說明，移除新片段必須按F固定門口的要求，列明已改自動繞行；補工作紀錄實際測試與限制，請使用者驗收。

Unity執行檔：`D:/unity/6000.5.0b9/Editor/Unity.exe`。背景Start-Process要Hidden，WaitForExit而不是-Wait；一次只開一個project process。不要commit／push／發布或刪舊資產。
測試命令不帶-quit（Coroutine完成自行Exit），build與工具方式沿用前一輪。

## 不要擴張

這是簡化接觸折線，不是完整軟體／流體模擬。牆截面快取目前針對靜態牆，動態換mesh／移牆要重新Configure。保持噴水、AI、計分、模型與相機不變；不另加MCP、不裝付費服務、不刪dirty worktree。原始 `.meta` GUID保留。
