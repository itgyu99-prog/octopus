using System.Collections.Generic;
using UnityEngine;

namespace AshenOath
{
    // Original procedural artwork. All sprites are generated locally; no external asset dependencies.
    public static class OathArt
    {
        public static readonly Color Gold = Hex("BEA77B"), Ink = Hex("10151E"), Pale = Hex("DED9CF");
        public static readonly Color[] Elements = { Hex("F88B54"), Hex("83CDE3"), Hex("A2BB72"), Hex("BEA0F3") };
        public static readonly string[] Names = { "잿불", "서리", "가시", "뇌광" };
        public static readonly string[] Titles = { "EMBER", "FROST", "BRIAR", "STORM" };
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Material lineMaterial;
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static Sprite Sprite(string name)
        {
            if (Cache.TryGetValue(name, out var found)) return found;
            const int n = 128; var pixels = new Color32[n * n];
            void Disk(float cx, float cy, float r, Color color)
            {
                for (int y = Mathf.Max(0, (int)(cy-r)); y < Mathf.Min(n,cy+r+1); y++)
                    for (int x = Mathf.Max(0,(int)(cx-r)); x < Mathf.Min(n,cx+r+1); x++)
                        if ((x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r) pixels[y*n+x]=color;
            }
            void Poly(Color color, params float[] xy)
            {
                for (int y=0;y<n;y++) for(int x=0;x<n;x++)
                {
                    bool inside=false; int len=xy.Length/2;
                    for(int i=0,j=len-1;i<len;j=i++)
                    {
                        float ax=xy[i*2],ay=xy[i*2+1],bx=xy[j*2],by=xy[j*2+1];
                        if((ay>y)!=(by>y)&&x<(bx-ax)*(y-ay)/(by-ay)+ax) inside=!inside;
                    }
                    if(inside) pixels[y*n+x]=color;
                }
            }
            var silver=Hex("B2BABF"); var dark=Hex("222936");
            switch(name)
            {
                case "knight":
                    Poly(Hex("433C55"),38,82,83,82,100,10,72,18,62,8,28,15);
                    Poly(Hex("65516E"),40,80,56,76,45,20,30,16);
                    Poly(dark,44,51,78,51,82,7,69,4,61,35,55,5,40,7);
                    Poly(silver,47,48,60,45,57,14,46,14); Poly(Hex("727D8C"),64,46,78,47,76,14,67,14);
                    Poly(Hex("535E70"),38,87,81,87,88,51,65,39,42,51);
                    Poly(silver,43,83,64,88,80,79,74,56,61,48,46,57);
                    Poly(Hex("E0D8BB"),60,80,65,80,65,54,60,54);
                    Poly(silver,32,84,43,90,49,77,39,67,28,73);
                    Poly(Hex("647283"),77,87,90,84,98,69,85,63,76,77);
                    Poly(dark,46,93,49,113,63,123,79,115,82,93,65,84);
                    Poly(silver,49,95,51,111,64,120,77,111,77,94,65,86);
                    Poly(Hex("E6E0CE"),62,118,66,118,68,95,62,92);
                    Poly(Ink,49,104,61,100,61,96,49,99); Poly(Ink,67,100,78,104,78,99,67,96);
                    Poly(Elements[1],50,102,60,99,60,97,50,100); Poly(Elements[1],68,99,77,102,77,100,68,97);
                    Poly(Gold,29,66,43,67,50,55,45,31,34,23,22,39,20,58);
                    Poly(Hex("263341"),29,62,40,63,46,53,41,35,34,28,26,41,24,55);
                    Poly(Gold,33,57,36,57,36,36,33,33); Poly(Gold,28,49,41,49,41,46,28,46);
                    Poly(Hex("E6E5DC"),99,87,103,107,108,86,108,32,99,32);
                    Poly(Hex("8495A0"),103,94,108,86,108,32,104,32);
                    Poly(Gold,93,31,114,31,115,27,92,27); Poly(dark,101,27,107,27,107,14,101,14);
                    break;
                case "shade":
                    Poly(Hex("333443"),22,7,35,40,33,70,49,98,65,112,83,94,96,64,88,39,107,9,76,18,65,6,43,17);
                    Poly(Hex("515260"),40,63,48,92,65,103,80,89,89,61,76,40,51,38);
                    Poly(Ink,49,71,53,89,75,91,80,71,73,55,57,57);
                    Poly(Hex("DD866B"),51,77,61,73,61,69,52,71); Poly(Hex("DD866B"),68,73,78,77,77,71,68,69);
                    Poly(Hex("77717C"),27,53,39,56,39,26,20,13,26,35); break;
                case "wraith":
                    Poly(Hex("40324F"),20,12,33,64,43,87,63,116,86,93,99,57,111,10,79,23,66,9,47,21);
                    Poly(Hex("6F547C"),37,61,46,84,63,108,84,86,91,61,69,43,48,46);
                    Poly(Ink,48,73,63,96,79,74,73,59,55,59);
                    Disk(58,75,3,Elements[3]); Disk(70,75,3,Elements[3]);
                    Poly(Gold,99,93,103,93,103,12,99,12); Disk(101,101,9,Elements[3]); break;
                case "boss":
                    Poly(Hex("502E3E"),8,3,23,55,34,91,94,91,107,57,122,3,80,12,64,1,39,13);
                    Poly(Hex("73636D"),26,84,42,93,87,92,104,77,94,39,67,25,35,41);
                    Poly(Hex("30313E"),41,86,64,96,88,84,82,48,64,37,47,48);
                    Poly(Gold,46,76,65,87,83,75,77,52,64,42,51,53);
                    Poly(dark,50,75,65,82,79,74,72,56,64,49,55,56);
                    Poly(silver,47,89,46,110,62,117,81,110,81,90,65,80);
                    Poly(Ink,48,102,78,102,74,94,52,94); Disk(57,99,3,Elements[0]); Disk(70,99,3,Elements[0]);
                    Poly(Gold,42,109,40,127,53,117,64,128,75,117,88,127,85,109);
                    Poly(silver,105,96,113,122,119,95,116,17,106,17); Poly(Gold,99,19,124,19,124,13,99,13); break;
                case "spirit":
                    Poly(Color.white,27,12,42,46,32,70,43,93,63,112,86,94,98,70,89,43,106,17,74,29,63,10,50,31);
                    Poly(new Color(.65f,.7f,.8f,.8f),37,65,45,87,63,105,82,89,92,66,77,48,51,48);
                    Poly(Ink,43,74,60,69,60,61,48,62); Poly(Ink,68,69,85,74,80,62,68,61);
                    Poly(Color.white,59,90,64,99,69,90,64,81); break;
                case "gem": Poly(Color.white,64,110,96,64,64,18,32,64); Poly(new Color(.5f,.6f,.7f),64,110,64,18,32,64); break;
                case "rune0": Poly(Color.white,61,118,90,75,100,48,85,20,58,12,32,31,29,57,46,85,46,56,61,71); break;
                case "rune1":
                    Poly(Color.white,59,12,69,12,69,116,59,116); Poly(Color.white,18,38,23,29,111,88,106,97); Poly(Color.white,18,88,23,97,111,38,106,29);
                    Poly(Color.white,45,96,64,78,83,96,83,105,64,88,45,105); break;
                case "rune2":
                    Poly(Color.white,60,12,69,12,69,108,60,108); Poly(Color.white,61,48,28,47,18,76,49,69,64,57);
                    Poly(Color.white,67,75,96,71,109,101,79,96,66,87); break;
                case "rune3": Poly(Color.white,71,119,26,58,57,58,45,8,103,78,73,78,86,119); break;
                case "glow":
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(64,64))/64f;pixels[y*n+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));} break;
                case "ring":
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(64,64));if(d>55&&d<59)pixels[y*n+x]=Color.white;} break;
                case "diamond": Poly(Color.white,64,126,126,64,64,2,2,64); break;
                case "stone":
                    var stoneRandom=new System.Random(41);
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                    {
                        float shade=.91f+(float)stoneRandom.NextDouble()*.09f;
                        if(x<2||y<2||x>125||y>125)shade=.58f;
                        else if(y>121)shade=1.08f;
                        pixels[y*n+x]=new Color(shade,shade,shade,1);
                    }break;
                case "vignette":
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++){float d=Vector2.Distance(new Vector2(x/64f-1,y/64f-1),Vector2.zero);pixels[y*n+x]=new Color(0,0,0,Mathf.Clamp01((d-.5f)*.65f));} break;
                default: Disk(64,64,61,Color.white); break;
            }
            var tex=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Oath_"+name,filterMode=FilterMode.Bilinear};
            tex.SetPixels32(pixels);tex.Apply();
            found=UnityEngine.Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),128); Cache[name]=found;return found;
        }
        public static SpriteRenderer Node(string name, Vector2 pos, Vector2 size, Color color, int order=0, Transform parent=null)
        {
            var go=new GameObject(name);if(parent)go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Sprite(name);sr.color=color;sr.sortingOrder=order;return sr;
        }
        public static LineRenderer Line(Transform parent, Color c, float width, int order, params Vector2[] points)
        {
            if(!lineMaterial)lineMaterial=new Material(Shader.Find("Sprites/Default"));
            var go=new GameObject("Etching");go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.sharedMaterial=lineMaterial;line.startColor=line.endColor=c;
            line.widthMultiplier=width;line.positionCount=points.Length;line.sortingOrder=order;
            for(int i=0;i<points.Length;i++)line.SetPosition(i,points[i]);return line;
        }
        public static Vector2 Polar(float a,float r=1) => new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
        public static LineRenderer Circle(Transform parent,Vector2 pos,float r,Color color,float width,int order=0,int sides=64)
        {
            var points=new Vector2[sides+1];for(int i=0;i<=sides;i++)points[i]=pos+Polar(i*Mathf.PI*2/sides,r);
            return Line(parent,color,width,order,points);
        }
        public static void BuildSanctum(Transform root)
        {
            var random=new System.Random(713);
            // The rectangular floor is deliberately unobstructed: all decorated pillars sit beyond the play boundary.
            Node("stone",Vector2.zero,new Vector2(100,100),Hex("111820"),-100,root);
            for(int y=-15;y<=15;y++) for(int x=-17;x<=17;x++)
            {
                float v=(float)random.NextDouble();
                var tile=Node("stone",new Vector2(x*1.5f+(y%2)*.75f,y*1.05f),new Vector2(1.49f,1.04f),
                    Color.Lerp(Hex("252C33"),Hex("30353A"),v*.65f),-95,root);
                if(v>.87f)Line(root,Hex("101820"),.022f,-94,(Vector2)tile.transform.position+new Vector2(-.25f,0),
                    (Vector2)tile.transform.position+new Vector2(.06f,.06f),(Vector2)tile.transform.position+new Vector2(.22f,-.13f));
            }
            var altars=new[]{new Vector2(-10,5),new Vector2(10,6),new Vector2(8,-8)};
            for(int i=0;i<altars.Length;i++)
            {
                Circle(root,altars[i],1.3f,new Color(.5f,.47f,.4f,.22f),.025f,-85,6);
                Circle(root,altars[i],1.6f,new Color(.5f,.47f,.4f,.12f),.025f,-85);
                for(int j=0;j<6;j++)Node("diamond",altars[i]+Polar(j*Mathf.PI/3,1.45f),Vector2.one*.1f,Gold*.45f,-84,root);
            }
            for(int i=0;i<3;i++)Circle(root,Vector2.zero,3.5f+i*.25f,new Color(.5f,.43f,.3f,.22f),.025f,-80);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;Line(root,new Color(.55f,.45f,.3f,.3f),.025f,-79,Polar(a,3.1f),Polar(a,3.4f));
                Node("diamond",Polar(a,3.8f),Vector2.one*.12f,Gold*.7f,-78,root);
            }
            Line(root,new Color(.5f,.43f,.3f,.16f),.03f,-80,Polar(Mathf.PI*.5f,3.1f),Polar(Mathf.PI*1.3f,3.1f),
                Polar(Mathf.PI*.1f,3.1f),Polar(Mathf.PI*.9f,3.1f),Polar(Mathf.PI*1.7f,3.1f),Polar(Mathf.PI*.5f,3.1f));
            for(int side=-1;side<=1;side+=2)for(int i=-4;i<=4;i++)
            {
                Vector2 p=new Vector2(i*4.7f,side*13.7f);
                Node("diamond",p+Vector2.down*.2f,new Vector2(2.7f,1.3f),Hex("0B1018"),-65,root);
                Node("diamond",p,new Vector2(1.5f,1.6f),Hex("4B4C4E"),-64,root);
                Node("diamond",p+Vector2.up*.35f,new Vector2(1,2.1f),Hex("353E47"),-63,root);
                Node("diamond",p+Vector2.up*.9f,new Vector2(1.65f,.65f),Hex("697078"),-62,root);
                Node("glow",p+Vector2.up*.9f,Vector2.one*4,new Color(.85f,.42f,.17f,.3f),-60,root);
                Node("rune0",p+Vector2.up*.95f,new Vector2(.22f,.4f),Elements[0],-59,root);
            }
            for(int i=0;i<65;i++)
            {
                Vector2 p=new Vector2((float)random.NextDouble()*43-21.5f,(float)random.NextDouble()*26-13);
                Node("glow",p,Vector2.one*(.8f+(float)random.NextDouble()*2),new Color(.25f,.44f,.46f,.055f),-72,root);
            }
        }
    }
}
