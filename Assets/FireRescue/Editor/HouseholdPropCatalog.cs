using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FireRescue.Editor
{
    // Greybox-only catalogue. Scene/gameplay roots own colliders and interactions;
    // every child below Visual is replaceable when the final art arrives.
    public static class HouseholdPropCatalog
    {
        public const string Folder = "Assets/FireRescue/Art/AIGenerated/Household";
        static readonly Dictionary<string, Vector3> Bounds = new Dictionary<string, Vector3>
        {
            { "StorageCabinet", new Vector3(1.1f, 1.4f, .42f) }, { "BakingSodaJar", new Vector3(.23f,.25f,.23f) },
            { "WashingMachine", new Vector3(.72f,.88f,.68f) }, { "Shower", new Vector3(.9f,1.9f,.9f) },
            { "Toilet", new Vector3(.68f,.82f,.82f) }, { "BathroomSink", new Vector3(.68f,.96f,.48f) },
            { "Bed", new Vector3(1.45f,.66f,2.05f) }, { "Wardrobe", new Vector3(1.25f,1.65f,.52f) },
            { "Desk", new Vector3(1.2f,.76f,.6f) }, { "Chair", new Vector3(.48f,.82f,.48f) },
            { "LowCabinet", new Vector3(1.15f,.6f,.42f) }, { "RecyclingBin", new Vector3(.42f,.58f,.42f) },
            { "TrashBin", new Vector3(.38f,.52f,.38f) }, { "Fridge", new Vector3(.78f,1.7f,.7f) },
            { "KitchenCounter", new Vector3(1f,.9f,.62f) }, { "Stove", new Vector3(.72f,.9f,.62f) },
            { "KitchenSink", new Vector3(.82f,1f,.62f) }, { "Sofa", new Vector3(2.15f,.86f,.82f) },
            { "Armchair", new Vector3(.9f,.86f,.85f) }, { "CoffeeTable", new Vector3(1.15f,.44f,.62f) },
            { "TvStand", new Vector3(1.25f,.54f,.43f) }, { "Television", new Vector3(1.02f,.72f,.16f) },
            { "EntryShoeCabinet", new Vector3(.96f,.78f,.32f) }, { "SodaCart", new Vector3(1.25f,1.18f,.66f) },
            { "SaladOilBottle", new Vector3(.22f,.42f,.22f) }
        };

        public static IEnumerable<string> Kinds => Bounds.Keys;
        public static string PrefabPath(string kind)
        {
            Require(Bounds.ContainsKey(kind), "Unknown household prop: " + kind);
            return Folder + "/" + kind + ".prefab";
        }
        public static Vector3 GetBounds(string kind)
        {
            Require(Bounds.ContainsKey(kind), "Unknown household prop: " + kind);
            return Bounds[kind];
        }

        // Deliberately idempotent: later artist replacement prefabs are never overwritten.
        public static void EnsureAll()
        {
            EnsureFolders();
            foreach (string kind in Bounds.Keys)
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(kind))) Create(kind);
            AssetDatabase.SaveAssets();
        }

        public static void ValidateAll()
        {
            foreach (string kind in Bounds.Keys)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(kind));
                Require(prefab, "Missing household prefab: " + kind);
                Transform visual = prefab.transform.Find("Visual");
                Require(visual && visual.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    visual.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, kind + " Visual must be presentation-only.");
                Require(prefab.layer == 8 && prefab.GetComponent<BoxCollider>(), kind + " needs only a root navigation collider.");
                Require(Vector3.Distance(prefab.GetComponent<BoxCollider>().size, GetBounds(kind)) < .002f, kind + " collider bounds changed.");
            }
        }

        static void Create(string kind)
        {
            GameObject root = new GameObject(kind) { layer = 8 };
            try
            {
                Vector3 size = GetBounds(kind);
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.size = size; collider.center = Vector3.up * size.y * .5f;
                Transform visual = Child(root.transform, "Visual");
                Build(kind, visual, size);
                Require(PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(kind)), "Could not save " + kind);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void Build(string kind, Transform v, Vector3 s)
        {
            Material wood = Mat("Wood", new Color(.48f,.34f,.22f));
            Material lightWood = Mat("LightWood", new Color(.68f,.51f,.32f));
            Material cream = Mat("Cream", new Color(.78f,.75f,.66f));
            Material white = Mat("ApplianceWhite", new Color(.72f,.75f,.74f));
            Material dark = Mat("ScreenDark", new Color(.07f,.09f,.1f));
            Material steel = Mat("Steel", new Color(.42f,.48f,.49f));
            Material green = Mat("RecycleGreen", new Color(.25f,.51f,.34f));
            Material blue = Mat("BathroomBlue", new Color(.35f,.57f,.62f));
            Material red = Mat("CapRed", new Color(.75f,.16f,.11f));
            Material yellow = Mat("OilYellow", new Color(.86f,.64f,.16f));

            switch (kind)
            {
                case "StorageCabinet": case "Wardrobe": case "LowCabinet": case "EntryShoeCabinet":
                    Box(v,"Case",new Vector3(0,s.y*.5f,0),s,wood);
                    Box(v,"Door L",new Vector3(-s.x*.245f,s.y*.52f,-s.z*.515f),new Vector3(s.x*.46f,s.y*.84f,.025f),lightWood);
                    Box(v,"Door R",new Vector3(s.x*.245f,s.y*.52f,-s.z*.515f),new Vector3(s.x*.46f,s.y*.84f,.025f),lightWood);
                    break;
                case "BakingSodaJar":
                    Cylinder(v,"Jar",new Vector3(0,.105f,0),new Vector3(.18f,.21f,.18f),white); Cylinder(v,"Blue lid",new Vector3(0,.225f,0),new Vector3(.2f,.05f,.2f),blue); break;
                case "SaladOilBottle":
                    Cylinder(v,"Oil",new Vector3(0,.15f,0),new Vector3(.18f,.3f,.18f),yellow); Cylinder(v,"Red cap",new Vector3(0,.35f,0),new Vector3(.09f,.1f,.09f),red); break;
                case "WashingMachine":
                    Box(v,"Body",new Vector3(0,s.y*.5f,0),s,white); Cylinder(v,"Round door",new Vector3(0,s.y*.47f,-s.z*.515f),new Vector3(.38f,.03f,.38f),dark,90); Box(v,"Controls",new Vector3(0,s.y*.83f,-s.z*.52f),new Vector3(s.x*.8f,.1f,.025f),steel); break;
                case "Shower":
                    Box(v,"Back wall",new Vector3(0,s.y*.55f,s.z*.43f),new Vector3(s.x,s.y*.9f,.08f),blue); Box(v,"Side wall",new Vector3(-s.x*.45f,s.y*.55f,0),new Vector3(.08f,s.y*.9f,s.z),blue); Cylinder(v,"Shower head",new Vector3(0,s.y*.86f,-s.z*.08f),new Vector3(.13f,.08f,.13f),steel); break;
                case "Toilet":
                    Box(v,"Cistern",new Vector3(0,s.y*.66f,s.z*.25f),new Vector3(s.x*.82f,s.y*.34f,s.z*.42f),white); Box(v,"Bowl",new Vector3(0,s.y*.28f,-s.z*.1f),new Vector3(s.x*.78f,s.y*.38f,s.z*.63f),white); Cylinder(v,"Seat",new Vector3(0,s.y*.48f,-s.z*.18f),new Vector3(s.x*.52f,.06f,s.z*.38f),cream); break;
                case "BathroomSink":
                    Box(v,"Base",new Vector3(0,s.y*.38f,0),new Vector3(s.x*.7f,s.y*.75f,s.z*.85f),white); Box(v,"Basin",new Vector3(0,s.y*.82f,0),new Vector3(s.x,s.y*.18f,s.z),cream); Cylinder(v,"Tap",new Vector3(0,s.y-.1f,s.z*.15f),new Vector3(.08f,.2f,.08f),steel); break;
                case "Bed":
                    Box(v,"Frame",new Vector3(0,s.y*.2f,0),new Vector3(s.x,s.y*.4f,s.z),wood); Box(v,"Mattress",new Vector3(0,s.y*.43f,0),new Vector3(s.x*.9f,s.y*.22f,s.z*.9f),cream); Box(v,"Pillow",new Vector3(0,s.y*.57f,s.z*.28f),new Vector3(s.x*.65f,.13f,s.z*.22f),white); break;
                case "Desk":
                    Box(v,"Top",new Vector3(0,s.y*.84f,0),new Vector3(s.x,.12f,s.z),wood); Legs(v,s,wood); break;
                case "Chair":
                    Box(v,"Seat",new Vector3(0,s.y*.46f,0),new Vector3(s.x,.11f,s.z),wood); Box(v,"Back",new Vector3(0,s.y*.75f,s.z*.36f),new Vector3(s.x,.42f,.1f),wood); Legs(v,new Vector3(s.x,s.y*.5f,s.z),wood); break;
                case "RecyclingBin": case "TrashBin":
                    Box(v,"Bin",new Vector3(0,s.y*.48f,0),new Vector3(s.x,s.y*.85f,s.z),kind=="RecyclingBin"?green:dark); Box(v,"Lid",new Vector3(0,s.y*.93f,0),new Vector3(s.x*1.05f,.08f,s.z*1.05f),kind=="RecyclingBin"?green:dark); break;
                case "Fridge":
                    Box(v,"Fridge",new Vector3(0,s.y*.5f,0),s,white); Box(v,"Upper door",new Vector3(0,s.y*.68f,-s.z*.51f),new Vector3(s.x*.88f,s.y*.55f,.025f),cream); Box(v,"Lower door",new Vector3(0,s.y*.25f,-s.z*.51f),new Vector3(s.x*.88f,s.y*.3f,.025f),cream); break;
                case "KitchenCounter":
                    Box(v,"Cabinet",new Vector3(0,s.y*.44f,0),new Vector3(s.x,s.y*.88f,s.z),wood); Box(v,"Countertop",new Vector3(0,s.y*.92f,0),new Vector3(s.x*1.04f,.07f,s.z*1.04f),cream); break;
                case "Stove":
                    Box(v,"Stove",new Vector3(0,s.y*.44f,0),new Vector3(s.x,s.y*.88f,s.z),steel); Cylinder(v,"Burner L",new Vector3(-s.x*.22f,s.y*.91f,-s.z*.13f),new Vector3(.22f,.025f,.22f),dark); Cylinder(v,"Burner R",new Vector3(s.x*.22f,s.y*.91f,-s.z*.13f),new Vector3(.22f,.025f,.22f),dark); break;
                case "KitchenSink":
                    Box(v,"Unit",new Vector3(0,s.y*.44f,0),new Vector3(s.x,s.y*.88f,s.z),wood); Box(v,"Basin",new Vector3(0,s.y*.91f,0),new Vector3(s.x*.68f,.07f,s.z*.62f),steel); Cylinder(v,"Tap",new Vector3(0,s.y-.1f,s.z*.15f),new Vector3(.08f,.2f,.08f),steel); break;
                case "Sofa": case "Armchair":
                    Box(v,"Seat",new Vector3(0,s.y*.34f,-s.z*.08f),new Vector3(s.x,s.y*.38f,s.z*.76f),cream); Box(v,"Back",new Vector3(0,s.y*.68f,s.z*.33f),new Vector3(s.x,s.y*.55f,s.z*.25f),cream); Box(v,"Arm L",new Vector3(-s.x*.43f,s.y*.5f,-s.z*.06f),new Vector3(s.x*.14f,s.y*.45f,s.z*.72f),cream); Box(v,"Arm R",new Vector3(s.x*.43f,s.y*.5f,-s.z*.06f),new Vector3(s.x*.14f,s.y*.45f,s.z*.72f),cream); break;
                case "CoffeeTable":
                    Box(v,"Top",new Vector3(0,s.y*.8f,0),new Vector3(s.x,.1f,s.z),lightWood); Legs(v,s,lightWood); break;
                case "TvStand":
                    Box(v,"Stand",new Vector3(0,s.y*.45f,0),s,wood); Box(v,"Shelf",new Vector3(0,s.y*.57f,-s.z*.51f),new Vector3(s.x*.85f,.04f,.025f),cream); break;
                case "Television":
                    Box(v,"Screen",new Vector3(0,s.y*.6f,0),new Vector3(s.x,s.y*.75f,s.z),dark); Box(v,"Frame",new Vector3(0,s.y*.6f,s.z*.52f),new Vector3(s.x*1.08f,s.y*.83f,.025f),steel); Box(v,"Foot",new Vector3(0,s.y*.16f,0),new Vector3(s.x*.32f,s.y*.24f,s.z*.85f),steel); break;
                case "SodaCart":
                    Box(v,"Cart",new Vector3(0,s.y*.35f,0),new Vector3(s.x,s.y*.5f,s.z),red); Box(v,"Canopy",new Vector3(0,s.y*.9f,0),new Vector3(s.x*1.1f,.12f,s.z*1.1f),cream); Cylinder(v,"Wheel L",new Vector3(-s.x*.42f,.14f,-s.z*.48f),new Vector3(.22f,.12f,.22f),dark,90); Cylinder(v,"Wheel R",new Vector3(s.x*.42f,.14f,-s.z*.48f),new Vector3(.22f,.12f,.22f),dark,90); break;
                default: throw new InvalidOperationException("No primitive recipe for " + kind);
            }
        }

        static void Legs(Transform parent, Vector3 s, Material mat)
        {
            foreach(float x in new[]{-.4f,.4f}) foreach(float z in new[]{-.4f,.4f})
                Box(parent,"Leg",new Vector3(s.x*x,s.y*.35f,s.z*z),new Vector3(.09f,s.y*.7f,.09f),mat);
        }
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static void Box(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat){Shape(parent,name,PrimitiveType.Cube,pos,scale,mat);}
        // Unity's primitive cylinder is two units tall. Recipes express real visible height,
        // so halve its local Y scale before optional sideways wheel/door rotation.
        static void Cylinder(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat,float xRotation=0)
        { scale.y *= .5f; var t=Shape(parent,name,PrimitiveType.Cylinder,pos,scale,mat);t.localRotation=Quaternion.Euler(xRotation,0,0); }
        static Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        static Material Mat(string name,Color color)
        {
            string path=Folder+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material) return material;
            material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.18f);AssetDatabase.CreateAsset(material,path);return material;
        }
        static void EnsureFolders()
        {
            if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/FireRescue/Art/AIGenerated","Household");
            if(!AssetDatabase.IsValidFolder(Folder+"/Materials")) AssetDatabase.CreateFolder(Folder,"Materials");
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
