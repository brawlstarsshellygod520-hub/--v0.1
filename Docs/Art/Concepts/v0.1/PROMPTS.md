# 概念圖生成紀錄

日期：2026-10-04  
模式：內建 imagegen，三個獨立新圖請求，非透明背景；未使用外部 CLI 或圖片參考。  
產出：`01-floorplan.png`、`02-firefighter.png`、`03-props.png`。  
用途：使用者審圖，非建模、非 Unity 成品。

以下保留實際送出的完整提示詞。三張已檢視：構圖完整、角色三視圖與物件可辨識；平面圖帶有俯視立體透視，僅用於空間提案，模型與尺寸合約以 CONCEPT_REVIEW.md 為準。

## 室內配置

```text
Create a polished game concept review board for a small single-room cooperative firefighting Unity prototype. LANDSCAPE board, warm light gray-beige background. Dominant image: a precise, clearly readable orthographic TOP-DOWN interior floorplan, NOT a perspective view pretending to be a floorplan. Small secondary 3/4 dollhouse inset in the lower right, with camera pitch about 50 degrees, showing matching room geometry. Style throughout: original rounded low-poly toy figurines, simple beveled meshes, smooth normals, matte vinyl surfaces, soft contact shadows, friendly party-game readability. No realistic textures, no sharp faceted surfaces.
Room is one rectangular workshop/storage room, approximately 12 meters wide and 10 meters deep. No roof. Entrance centered on bottom wall, clearly open and about 3 meters wide. Outside the entrance is a small staging yard: fixed BLUE WATER outlet on the left, fixed BROWN OIL outlet on the right, GREEN EXIT pad between them near the doorway. Keep sources separate from the exit and leave a wide walkable path. Back of room: one small civilian NPC near the center. Furniture: rounded workbench against left wall, rounded storage cabinet against right wall, one movable wooden crate in the front-right quarter. Central travel corridor at least 2.5 meters wide; simple room with ample space, no extra bedrooms or kitchens.
Show a transverse initial fire hazard band across the middle of the room from left to right, composed of small orange stylized flame markers with a translucent orange floor zone. In the middle of that band, show a narrow cyan proposed water-cleared opening, labeled as a gameplay proposal, and a thin green dotted evacuation arrow from the NPC through that center opening and entrance to the exit pad. Arrow must not run through walls or furniture. A single rounded firefighter at the entrance connects to the water outlet using a dark hose. Do not depict a firetruck, complex pipes or network UI.
Sparse, legible Chinese annotations placed OUTSIDE geometry with neat leader lines: “入口”, “水源”, “油源”, “出口”, “NPC”, “可推箱”, “初始火帶”. Dimension arrows marked “12 m” and “10 m”. Title “消防 Prototype｜室內平面配置”. The inset is a clean matching room with low foreground walls so the player remains visible. This is an approval-ready visual concept sheet, not an engineering drawing. Prioritize geometrical clarity, open door, broad passages and restrained labeling.
```

## 消防員

```text
Create an approval-ready CHARACTER MODELING CONCEPT SHEET for an original rounded low-poly toy firefighter for a cooperative Unity party game. LANDSCAPE canvas, warm light gray-beige studio background, generous whitespace. Main section: three complete full-body orthographic views of EXACTLY THE SAME character: FRONT, SIDE, BACK, arranged left to right at identical height and consistent proportions. Front and back in relaxed modeling A-pose with arms slightly away from torso, no weapons or hose blocking body. Side is true profile. On the right a larger three-quarter full-body action vignette of that same firefighter holding a short dark-gray hose nozzle in both hands, dark hose trailing on ground. All views must be consistent.
Design: cute chunky vinyl collectible figurine, approximately 3 heads tall, oversized ROUND YELLOW firefighter helmet with broad curved brim, simple friendly face with tiny dark eyes, rounded warm RED-ORANGE coat and trousers, two broad pale-yellow reflective bands, mitten-like dark gloves, short thick limbs, large rounded dark charcoal boots. Simple identifiable silhouette readable from an elevated game camera. Helmet, sleeves, boots and body are separate simple low-poly-friendly volumes with broad bevels and smooth shading. No sharp facets, no photorealism, no gritty textures, no cloth wrinkles, no complex breathing apparatus, no tiny badges, no complex harness, no celebrity likeness. The head and helmet shape must match in every view. Back shows plain coat with broad reflector, no huge backpack.
Palette: warm red-orange, buttery yellow, charcoal, soft ivory. Matte surfaces and soft compact contact shadows. Small color swatches along bottom showing red, blue, green and yellow as OPTIONAL future player accent colors, not four unrelated character designs. Minimal labels “正面”, “側面”, “背面”, “持管姿勢”. Title “消防員｜圓潤低模公仔”. Make the proportions and construction clear enough for later modeling, while remaining a clean charming game art proposal.
```

## 物件

```text
Create an approval-ready GAME PROP MODELING CONCEPT BOARD for an original cooperative firefighting Unity prototype, matching rounded low-poly toy figurines. LANDSCAPE canvas, warm light gray-beige studio background, generous whitespace, soft contact shadows. Seven clearly separated asset groups in a clean two-row grid, each shown in an elevated three-quarter view with consistent lighting and style. No actual game UI. Matte vinyl/plastic finish, broad rounded bevels, smooth normals, simple low-poly-friendly forms, large readable color blocks, no photoreal textures, no sharp faceted polygons, no clutter.
Asset groups:
1. WATER SOURCE: short chunky fixed blue water tank/outlet, rounded body, stable flat base, one large accessible metal hose socket on front and simple white droplet icon.
2. OIL SOURCE: short chunky fixed brown/amber oil reservoir, similar functional hose socket but a visibly different squarer silhouette and amber stripe, simple dark droplet icon. Not a firetruck or fuel station.
3. HOSE KIT: one charcoal flexible hose arranged in a simple coil beside one sturdy short dark-gray nozzle with rounded blue grip and a simple matching connector. Only one hose, simple readable nozzle, no complicated machinery.
4. PUSHABLE CRATE: rounded wooden-colored block crate, broad structural bars, no wood-grain texture, approximately knee to waist high next to the character scale.
5. WORKBENCH: broad rounded pale wooden worktop on chunky charcoal supports, open space beneath, no tiny scattered tools.
6. STORAGE CABINET: compact rounded blue-gray low cabinet, two simple doors with large handles, no open complicated shelves.
7. RESCUE NPC: simple civilian toy figurine with round head, teal shirt, beige trousers, short chunky limbs and dark rounded shoes; standing neutral pose, no firefighter helmet, visually distinct from the red-orange firefighter.
Chinese labels beneath groups: “水源”, “油源”, “消防管＋噴嘴”, “可推箱”, “工作台”, “儲物櫃”, “待救 NPC”. Title “消防 Prototype｜核心物件”. Show coherent family of modular game assets, plausible shared scale and restrained detail. Source sockets must visibly match the hose connector. Make silhouettes unambiguous for later low-poly modeling. No flames, environment floorplan, logos, decorative vehicles, text paragraphs, or additional systems.
```

