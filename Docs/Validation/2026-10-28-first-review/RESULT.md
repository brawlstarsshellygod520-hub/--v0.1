# 一審住宅展示版：驗證結果（2026-10-28）

狀態：核心流程與Windows建置可交付驗收；**不是完整美術／手感已驗收**。使用者要求先不做螢幕控制，已停止後續鍵鼠與互動視窗操作。

## 最終證據

| 項目 | 結果／檔案 |
|---|---|
| 新關卡完整Play Mode＋重開及邊界 | **81 PASS**，`playmode.txt` |
| 舊卡牆／5m／來源生命週期 | **22 PASS**，`regression-hose.txt` |
| 自動繞牆／退回解繞／柱子迴圈 | **29 PASS**，`regression-wrap.txt` |
| 每刀一次／門破碰撞／統計 | **21 PASS**，`regression-door.txt` |
| 合計 | **153項自動檢查**，不包含照片、建置或人工觀察 |
| 出生平台涵蓋 | `presentation.txt`；48m地面覆蓋16:9／21:9視錐四角，不改用戶戶型 |
| Windows x64 Development／Mono | `build.txt`：Succeeded，0 errors／0 warnings |
| Windows實際D3D11載入 | **14 PASS／REVIEW_PLAYER_PASS**，`player-smoke.txt`（執行打包exe，不是只有Editor）；黑圖明示不驗證UI |
| 交付入口 | `D:/Documents/畢專/PlayLatest.cmd` → `Builds/Latest/FireRescue.exe`，BUILD_INFO含「2秒油火擴張／15m連續3D消防管」 |

測試／建置方法：`FirstReview1027Validation.BatchRun`、`HosePhysicsValidation.BatchRun`、`HoseWrapValidation.BatchRun`、`BreakableDoorValidation.BatchRun`、`FirstReview1027Setup.BatchBuild`；玩家smoke參數 `--verify-review -force-d3d11`。背景程序Hidden，自行退出，不開其他專案、不commit／push。

## 主流程真正測了什麼

出生先等待實際模擬2秒，油火範圍到達布丁，CarryItem.Take也拒絕拿取。以同一PointerCommand／CharacterController走正門、浴室接水、拉管繞牆進廚房；真飛行樣本命中鍋，強度1→3。斷管、走倉庫接蘇打粉、沿原門洞返回、持續噴射滅火，熱區清除。三刀破用戶房門，未交布丁仍不逃；E拿布丁、持物不能用工具，穿已破門交到實際骨頭上，世界尺寸不因FBX100x放大，NPC步行抵達屋外出口、完成只一次。重開的火／門／物品／要求／倒數／管線全重置。

新3D管身測到128以上節點與八邊形網格、有限頂點／節點距離、兩個來源長路徑與拓撲改變時不中途隱藏；舊LineRenderer呈現在此關停用。管線權威長度／繞牆仍是HoseConnection；PBD節點只負責物理感呈現，不將視覺碰撞當斷線。

邊界包括精確60秒失敗、非有限時間／水量、晚交不能翻回成功、水增火上限、粉末熄火後水不重燃、未啟用水增火的舊油火不變、持物F仍可拔管、來源停用、端點轉向、過長限制、單次完成及清潔重開。直線步態平均站立腳平移約0.00063m/s，持槍Idle目標誤差<3cm、移動<6cm；不代表所有轉身／急停零滑步。

## 畫面證據與明確缺口

- 已實際檢視Unity D3D11背景渲染：`01-layout.png`、`oil-spread-2s.png`、`02-water-mistake.png`、`continuous-3d-hose.png`、`pose-idle.png`、`pose-hose.png`、`pose-hose-walk.png`、`pose-axe.png`、`pudding-delivered.png`。背景Camera.Render不包含OnGUI，不可稱新NameTag已視覺驗收。
- 使用者停止控制前，曾觀察Windows繁中HUD、倒數並實按R見01:00；`windows-ui-observed-before-ground-fix.png`也來自使用者。之後Escape明確中斷，未再注入鍵鼠；不能稱Windows全程人工通關。
- Hidden Windows的ScreenCapture可能全黑，已在smoke程式註明「檔案存在不是視覺PASS」並偵測黑圖，不把此當作畫面證據。
- 新NameTag附NPC／投影／隱藏條件有靜態與載入檢查，但仍需使用者看實際位置與可讀性；正式四件模型尚未提供。

## 品質回合中抓到並修正

1. 測試路徑最初撞到馬桶、跨到廚房實體側牆：修自動測試的走法，不刪用戶牆／放大門求通過，失敗留 `before-toilet-approach.txt`、`before-route-correction.txt`。
2. FBX左右與actor左右相反，手臂交叉變形：依肩膀實際位置正規化；衣襬／飾條綁腿及缺前臂權重另做同拓撲Skin衍生檔，原FBX不動。`before-grip-correction.txt`保留失敗。
3. 布丁掛100x骨頭會放大：SetParent保留世界尺度，新增交付尺寸驗證。
4. 持布丁時曾無法F拔管：允許只斷線不換工具，新增獨立fixture。
5. 右側灰底來自原22m地面邊緣：只延伸地面，視錐覆蓋檢查通過。
6. 舊線一小段擦牆就整條隱藏：新連續TubeMesh；PBD壓縮曾產生鋸齒，改距離上限＋彎曲平滑，長段落地、接點才抬高。

Unity編輯器仍有歷史驗證工具使用過時FindFirstObjectByType的CS0618警告；未為本次修整做無關清理。最終BuildReport為0警告，不代表整個dirty工作樹或所有歷史Editor工具無警告。原Source Blend SHA256與Downloads一致；既有場景／資產GUID保留。

## 請玩家驗收

1. 開場2秒後不能直接E拿布丁，用水火更大、粉末才能清火拿取。
2. 15m管線繞門角、貼地、進退與快速轉向，不應整條閃斷；藏在牆後的局部自然遮擋不等於斷線。
3. NPC要求在頭上，交付後改文案、逃出後消失；60秒是否適合初見者。
4. 持槍／斧的手感、穿插與視角；之後正式模型的Visual／接點／碰撞替換再重驗。

油火區目前共用一個油鍋強度，並非每格獨立火；無玩家血量／火傷害。管線為簡化Verlet/PBD視覺＋權威折線，不是完整軟管剛體、流體或多人同步。MCP Bridge未連線，本輪未繼續安裝；不影響上述本機證據。
