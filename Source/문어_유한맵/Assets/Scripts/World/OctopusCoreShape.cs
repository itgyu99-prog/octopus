using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    // A coarse alpha mask of the actual portrait avoids damage in its transparent corners.
    public static class OctopusCoreShape
    {
        static readonly List<Vector2> cells = new List<Vector2>();
        static float padding;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { cells.Clear(); }
        static void Ensure()
        {
            if(cells.Count>0)return;
            var data=Resources.Load<TextAsset>("OctopusBoss/Body");
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            ImageConversion.LoadImage(texture,data.bytes);
            var pixels=texture.GetPixels32();
            const int grid=64;
            float scale=OctopusBossPattern.PortraitScale/100;
            padding=Mathf.Max(texture.width,texture.height)/(float)grid*scale*.71f;
            for(int y=0;y<grid;y++)for(int x=0;x<grid;x++) {
                int px=Mathf.Min(texture.width-1,(int)((x+.5f)*texture.width/grid));
                int py=Mathf.Min(texture.height-1,(int)((y+.5f)*texture.height/grid));
                if(pixels[py*texture.width+px].a>128)
                    cells.Add(new Vector2(px-texture.width*.5f,py-texture.height*.5f)*scale);
            }
            Object.Destroy(texture);
        }
        public static float Distance(Vector2 p,Vector2 center) => SegmentDistance(p,p,center);
        public static float SegmentDistance(Vector2 a,Vector2 b,Vector2 center)
        {
            Ensure(); float best=float.MaxValue; var ab=b-a;
            foreach(var cell in cells) {
                var p=center+cell;
                float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.00001f,ab.sqrMagnitude));
                best=Mathf.Min(best,(p-a-ab*t).sqrMagnitude);
            }
            return Mathf.Max(0,Mathf.Sqrt(best)-padding);
        }
    }
}
