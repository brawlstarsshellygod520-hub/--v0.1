# 室內 Blockout 簡化 — 生成紀錄

日期：2026-10-04  
模式：內建 imagegen，圖片局部風格簡化，非透明背景。未使用 CLI。

輸入：`01-residential-layout.png`（住宅第二版細稿）。  
最終檔案：`01-residential-blockout.png`。

視覺檢查：保留三房一廳一衛、開放廚房、門口連通與水箱／沙拉油來源位置；移除植物、衣櫃、床邊桌、地毯、室外景觀等裝飾，以簡單色塊家具及 Capsule 代表角色。未修改消防員或物件概念圖，未建模／整合 Unity。

圖片仍為配置示意，不保證工程比例；只調低室內圖細節，不變更既有玩法合約。

## 完整提示詞

```text
Use case: style-transfer / simplification of an existing interior concept image.
Input image is the EDIT TARGET. Keep its ROOM LAYOUT and gameplay source positions, but dramatically REDUCE DETAIL to an extremely simple Unity primitive BLOCKOUT review image, not a finished interior illustration.
Landscape board, plain warm-white background. A near-vertical orthographic top-down view with only slight visible extrusion of low walls. Simple solid flat colors, untextured surfaces, minimal soft shadow. Broad rounded corners, no sharp facets, no photoreal textures.
Preserve: exactly three separate bedrooms across the top, broad central corridor, one living room in lower-left, one enclosed bathroom in lower-right, small open kitchen nook as part of living room, front main door at bottom. Each bedroom has exactly one clearly open doorway to corridor; bathroom opens to corridor; living room connects to corridor and front door. Keep all door gaps physically readable. Same proposed 16m by 14m and corridor about 2m clear. Do not add rooms or change positions.
SIMPLIFY EVERY OBJECT:
- Walls: uniform plain pale-gray low extruded rectangles, no trim, pillars, tile, brick or stone.
- Each bedroom: ONLY one rounded rectangular bed block and one small pillow block. Remove nightstands, lamps, dressers, wardrobes, shelves, books, rugs, plants, pictures and all decor.
- Living room: ONLY one plain rounded sofa block and one low rectangular table block, with nothing on it. No cushions, armchair, TV or ornaments.
- Kitchen nook: ONLY a simple cream counter block, one sink shape and ONE SMALL golden cooking-oil bottle on the counter. Oil bottle should be recognizable as ordinary kitchen cooking oil, not an industrial tank.
- Bathroom: simplified normal white toilet with CLOSED OVAL SEAT and separate OPEN RECTANGULAR CISTERN behind it, a blue flat surface INSIDE THE CISTERN indicates water. Small plain sink block and simple shower-square marker. No tiles, glass hardware, radiators, little decorative details.
- One orange capsule player marker in bathroom and one teal capsule NPC marker in middle bedroom. NO faces, clothing, helmet modeling or detailed people.
- If shown, hose is ONLY a short plain charcoal curve leading into the RECTANGULAR TOILET CISTERN, not the bowl. No connector detail.
- Fire: two or three small flat orange/red hazard markers near middle-bedroom doorway; no realistic flame or smoke.
- Exterior: ONLY a blank small entry strip, no plants, lawn, pavers, flowers, shrubs or landscaping.
No evacuation arrows, no complicated overlays or long paragraphs. Sparse clean Chinese room labels “臥室 1”, “臥室 2”, “臥室 3”, “走廊”, “客廳”, “浴室”, “開放式廚房”, “正門”; small callouts “水源：馬桶水箱” and “油源：沙拉油瓶”. Title “室內 Blockout｜三房一廳一衛”. Dimensions can be minimal simple lines marked “16 m”, “14 m”, “約 2 m”.
The visible simplicity is essential: a planning mockup made of a few chunky primitive shapes, approximately 80 percent less decorative detail than input. NOT a detailed dollhouse render. Preserve the house connectivity and everyday household source concept.
```
