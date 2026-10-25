# 消防斧＋油鍋乾粉：驗收結果

執行日期2026-10-21，彙整2026-10-22。Unity6000.5.0b9／Built-in／Windows DX11，Blender5.2.2。

## 最終結果

| 入口 | 結果 |
| --- | --- |
| UserAxeSetup.BatchInstallAndValidate | exit0；匯入、保存／重開、重跑不覆寫、刀刃朝前、原玩法序列化不變 |
| PowderGameplayBuilder.BatchInstallAndValidate | exit0；Source／Fire種類、三個來源接線、模型碰撞分離、保存重開／冪等 |
| UserAxeValidation.BatchRun | exit0；16項Play Mode PASS |
| PowderGameplayValidation.BatchRun | exit0；36項Play Mode PASS |
| WaterGameplayValidation.BatchRun | exit0；27項實景回歸PASS |
| WaterReactionValidation.BatchRun | exit0；25項反應回歸PASS |

共104項Play Mode檢查。最終六份Logs無C# error、Exception或FAIL；舊CS0618警告仍有。已檢視真Game Camera畫面，包括最後修正刀刃方向的斧頭近景、乾粉噴射與熄滅。近景僅供QA，未修改正式鏡頭。

- 新可玩路徑實際使用enabled LocalPlayerInput共用命令：屋外→浴室F接水→拉管→螢幕瞄準檯面鍋子（水無效）→F換乾粉→連續噴射逐漸至0。沒有直接呼叫Receive代替核心可玩路徑。
- 負向驗證：牆擋噴流／範圍反應、鬆鍵停止、無來源無效果、粉不加濕／不推物、整個Visual關閉仍滅火。斧頭關閉模型仍命中、左右鍵切換與優先正常。
- 獨立唯讀review未發現指定Runtime／Builder／Test範圍的P1/P2問題；主代理審查實際腳本、場景diff與驗收圖，不只採用代理摘要。

## 過程修正

- 匯出先在原scene烘焙Skin/Subdivision，再建FBX場景；修正空白preview和漏柄。
- 保留單斧6拆件；寬刃在source+X。依Unity實際導入方向修正180度反向，新增切刃材質頂點方向檢查。舊近景保留在before-direction-swing.png。
- 初始GetInstanceID／EntityId隱式轉int編譯失敗已改成GetEntityId().ToString()。兩份compile failure及一次QA座標空間failure保存供追查，最終均重跑通過。
- 斧頭全長.720m，柄尾-.120m；使用者.blend與副本SHA256一致。完整保護比對見change-audit.json：370個既有檔中6改、364不變、0刪除；新增32個Assets檔含meta、Missing Meta=0。
- 既有Prefab、其他場景、原模型、GUID、Packages／Settings不改；沒有commit、push或清理dirty worktree。

## 邊界

仍單人、無限來源、直線13m供應管；未做窗口路由／破門／UI／正式握持動畫。獨立鍋火尚無地板蔓延／復燃／NPC額外條件。未測實體鍵鼠、Player build或多人。
操作文件：Docs/POWDER_GAMEPLAY.md、Docs/USER_FIRE_AXE.md。工作紀錄：Docs/WORKLOG.md。
