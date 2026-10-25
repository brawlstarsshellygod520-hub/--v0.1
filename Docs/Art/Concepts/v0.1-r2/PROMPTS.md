# 住宅概念圖第二版 — 生成紀錄

日期：2026-10-04  
模式：內建 imagegen；非透明背景；以本地上一版圖作修改目標與角色風格參考。未使用 CLI。

最終檔案：
- `01-residential-layout.png`：住宅配置；來源為 `exec-d01be324-5755-47fc-843a-2cd96e497ede.png`。
- `03-household-props.png`：住宅物件；來源為 `exec-ff1dc7d8-5de4-44cb-bd09-2d3cd7b17b50.png`。
- 消防員沿用 `../v0.1/02-firefighter.png`，未重新生成。

檢查／修正：首張住宅圖錯把水放在馬桶盆，第二次局部修改改成水箱，補客廳標籤並合併臥室 2 額外牆縫；修正後綠色動線仍穿牆，最後一次局部修改移除所有路線箭頭。最終圖以文件敘述救援路線，不將錯誤箭頭留在審圖稿。原始房型尺寸仍以概念文件為準，無工程比例保證。

## 住宅配置 — 初稿完整提示詞

參考 1：`../v0.1/01-floorplan.png`，舊圖重設目標。  
參考 2：`../v0.1/02-firefighter.png`，角色／風格參考。

```text
Use case: stylized-concept / residential floorplan revision.
Input Image 1 is the old room-layout board to REDESIGN completely; keep only its warm beige presentation and rounded toy style, NOT its one-room layout or workshop objects. Input Image 2 is firefighter identity/style reference: keep yellow helmet and red-orange uniform.
Primary request: approval-ready interior floorplan for a funny cooperative firefighting party game: the fire engine has NO WATER, so firefighters must go INSIDE a real home and improvise by attaching their hose to household fluids. ONLY Water from a TOILET CISTERN and Oil from ONE ordinary bottle of cooking/salad oil. No industrial tanks.
Composition: landscape board. Main drawing is a TRUE ORTHOGRAPHIC TOP-DOWN floorplan: camera looks vertically down, walls parallel, rectangular plan with no trapezoid perspective. Rounded 3D toy furniture is visible from directly above. Do not add a second full plan or perspective inset. This must clearly be a complete THREE BEDROOM, ONE LIVING ROOM, ONE BATHROOM house, NOT a workshop.
Plan: approximately 16m wide by 14m deep, simplified game-scale. Three distinct closed bedrooms in the back/top row, each with one bed and its own clearly visible door opening to a wide horizontal central corridor. Corridor about 2m clear. Front-left larger living room with a sofa and low coffee table; small OPEN-PLAN KITCHEN nook on the living room side, with a short normal sink/cooking counter and ONE SMALL transparent bottle of golden SALAD OIL standing on that counter. Kitchen is part of the living space, NOT a fourth bedroom or second living room. Front-right one enclosed bathroom, with a normal WHITE PORCELAIN TOILET, sink and simple shower. Show the toilet's rectangular cistern behind the seat with its lid lifted and WATER visible INSIDE THE CISTERN; the cistern is the WATER SOURCE, not the bowl. Bathroom has ONE door opening into central corridor. Living room has a broad opening to corridor. Front main door at bottom opens into living room and leads to outdoor footpath; this is the NPC exit, no glowing source pads or crates outside.
Every bedroom and bathroom must connect via actual gaps in walls to corridor, corridor to living room, living room to front door. Keep all furniture clear of those openings. Beds and door swings cannot obstruct the 2m corridor. Low rounded foreground walls / open roof, no invisible walls.
Show one small civilian NPC in center bedroom, a small firefighter near the bathroom, and a short charcoal hose entering the OPEN toilet cistern. Place a few small stylized orange fire markers near NPC bedroom door and one living-room corner, NOT across the entire building. Entry-to-bathroom access remains clear so a player can obtain water. Thin green dashed proposed evacuation line from NPC through its bedroom doorway, corridor, living room, front door to outdoor path; never passes through walls or furniture. No water-cleared opening preprinted as already safe.
Sparse legible annotations ONLY on this review drawing, not game UI: “臥室 1”, “臥室 2”, “臥室 3”, “客廳”, “浴室”, “走廊”, “開放式廚房”, “正門”. Leader lines for “水源：馬桶水箱” and “油源：沙拉油瓶”. Title “消防車沒水了｜三房一廳一衛”. Small dimension labels “16 m”, “14 m”, “走廊淨寬約 2 m”. Labels do not hide objects.
Style: rounded low-poly collectible toy game, simple meshes with broad bevels and smooth normals, matte vinyl/clay-like materials, warm cozy household colors, soft contact shadows, warm light beige board background. Restrained detail. Avoid industrial reservoirs, blue source barrels, brown source barrels, fire hydrants, workshop workbenches, storage cabinets, warehouses, laboratory equipment, giant oil bottles, fourth bedroom, second bathroom, second living room. Keep the humor in using ordinary domestic objects.
```

## 住宅物件 — 完整提示詞

參考 1：`../v0.1/03-props.png`，舊圖修改目標。  
參考 2：`../v0.1/02-firefighter.png`，角色／風格參考。

```text
Use case: precise-object-edit / residential game props concept board revision.
Input Image 1 is the previous seven-group prop board to revise: remove BOTH industrial fluid tanks, remove workbench, storage cabinet and shipping crate. Keep rounded toy styling and a simple hose/nozzle design, but household props now define the game. Input Image 2 is the established yellow-helmet red-orange firefighter style reference.
Create a LANDSCAPE approval-ready 3D prop concept sheet, warm light gray-beige studio background, soft compact contact shadows, clean whitespace, individually separated groups in a grid. Style: rounded low-poly toy/vinyl figurines, simple meshes, broad bevels, smooth normals, matte finish, large readable shapes, no sharp faceting, no gritty realism.
The game premise is: FIRE ENGINE RAN OUT OF WATER, firefighters go inside a three-bedroom residence to find ordinary household sources for their hose. TWO fluid sources only, no specialized reservoir props.
Eight groups, no other objects:
1. WATER SOURCE: an immediately recognizable normal white porcelain TOILET with rectangular CISTERN behind the seat. Cistern lid visibly lifted or set aside, soft blue water visible INSIDE TANK. Bowl stays distinct. A simple removable dark hose end dips into cistern from above. Do NOT add a permanent industrial port, pumping machine, glowing base or giant water jug. Water comes from the toilet TANK, not the bowl.
2. OIL SOURCE: ONE normal small household plastic SALAD/COOKING OIL BOTTLE, transparent light golden body, yellow oil, small green or yellow screw cap, simple paper label bearing “沙拉油”. Ordinary kitchen bottle silhouette and scale: tiny compared to toilet, no industrial barrel, no huge jerrycan, no oil drum. Show cap beside bottle and a simple small removable hose adapter at mouth as a playful improvised connection; label stays visible. Do not redesign it as specialist fire equipment.
3. CHARCOAL HOSE AND SHORT NOZZLE: simple hose coil and chunky gray nozzle with grip, plus tiny removable clip/adapter; accessible party-game forms, no complex pump. Large hose tapers at optional bottle adapter, not a bottle wider than a player.
4. LIVING-ROOM SOFA: rounded domestic two-seat warm muted coral sofa, simple cushions, no tiny details.
5. LOW COFFEE TABLE: rounded light-wood domestic low table, open floor space under it.
6. BED: simple rounded bed with pillow and mint blanket, clean simple geometry; this one model can serve three bedrooms.
7. KITCHEN COUNTER MODULE: compact domestic cream-colored cooking/sink counter with a sink and simple cooktop, not a workshop workbench; no unrelated tools or industrial cabinet.
8. RESCUE NPC: simple civilian rounded toy person with teal shirt, beige trousers and dark shoes, matching prior civilian identity, neutral standing pose.
Labels only below objects: “水源：馬桶水箱”, “油源：沙拉油瓶”, “消防管＋噴嘴”, “沙發”, “茶几”, “床”, “廚房流理台”, “待救 NPC”. Title “室內找來源｜住宅核心物件”. Use clean sparse typography. Sources should be most prominent; include a subtle tiny 1m scale bar and a clear silhouette of ordinary bottle height relative to toilet, so cooking oil is not depicted as a giant tank. The assets should read as things naturally found in a home, not game supply stations. No industrial fluid sources, workbench, warehouse cabinet, source pedestals, shipping crate, truck, new characters, logos or extra reaction types.
```

## 配置圖 — 水箱、標籤與門口修正

參考 1：配置初稿 `exec-f2090b84-338b-441c-af0d-ea05b3fdd5b8.png`。  
參考 2：新住宅物件圖，提供正確馬桶水箱外形。

```text
Use case: precise-object-edit. Image 1 is the floorplan EDIT TARGET. Image 2 is a supporting reference for the CORRECT TOILET SOURCE DESIGN, not a new scene.
Preserve Image 1's three bedrooms, one living room, one bathroom, open kitchen, corridor, normal small cooking oil bottle, firefighter, ordinary residential furnishings, labels, warm rounded low-poly toy look, title and dimensions. Make ONLY these localized corrections:
1. IMPORTANT: In the BATHROOM at lower right, the hose currently points into a blue-filled toilet BOWL. This is WRONG. Replace that toilet with the normal white toilet in Image 2: CLOSED OVAL SEAT LID in front; separate OPEN RECTANGULAR CISTERN behind the seat containing blue water. Hose end goes into the RECTANGULAR CISTERN FROM ABOVE, NOT into the closed seat or bowl. Put cistern lid leaning beside tank. Water-source leader line must point to cistern. Turn or enlarge this toilet slightly if necessary so the open rectangular water tank is clearly visible in the plan. Keep sink, shower, room layout and firefighter position.
2. Add the simple white rounded label “客廳” in an empty patch of the large front-left living space, near but not over the sofa/coffee table.
3. The center bedroom has a wooden doorway on the LEFT of its front wall and a second bare gap where the green dotted line currently passes. CLOSE ONLY THAT EXTRA BARE GAP with matching low wall. Retain the center bedroom's existing wooden door opening to corridor. Reroute the green dashed NPC escape line through that ACTUAL WOODEN DOOR OPENING on left, then along the clear corridor and existing living-room opening to front door. Do not pass the route through any wall or furniture. Keep three bedrooms and all other walls/doors unchanged.
Do not move the oil bottle outside. Do not add industrial tanks, cabinet/workbench, new rooms, other fluid sources or extra labels. Preserve whole-board framing and 16m/14m/2m annotations.
```

## 配置圖 — 移除路線標記（最終）

參考：修正稿 `exec-95954330-7935-473c-995e-d65557ebf451.png`。

```text
Precise localized edit of this residential floorplan concept board. REMOVE ALL GREEN DASHED LINES AND GREEN ARROWHEADS from the center bedroom, hallway, living-room floor and outdoor path. Restore normal uninterrupted floor textures beneath those markings. Do not replace them with any other route or overlay. Make NO OTHER CHANGES. Preserve the exact three bedrooms, their wooden doors, corridor, single bathroom, OPEN RECTANGULAR TOILET CISTERN with blue water and hose inserted into CISTERN rather than closed seat, one living room label “客廳”, open kitchen with ONE SMALL cooking oil bottle, sofa, beds, firefighter, civilian NPC, walls, plants, fire markers, Chinese labels, dimensions, title and existing rounded low-poly toy style. Keep the whole image composition and framing unchanged. This is a clean interior layout review board, not an escape-route diagram.
```

