using UnityEngine;

namespace OctopusSurvivors
{
    // World-space coverage follows the authored sandy field, never the camera or caster.
    public static class CelestialField
    {
        public static Bounds Bounds => BoundsFor(BeachGeometry.BossMode);
        public static Bounds BoundsFor(bool boss)
        {
            Vector2 a = BeachGeometry.World(new Vector2(.18f, boss ? .21f : .425f), boss);
            Vector2 b = BeachGeometry.World(new Vector2(.82f, boss ? .61f : .82f), boss);
            return new Bounds((a+b)*.5f, new Vector3(Mathf.Abs(b.x-a.x), Mathf.Abs(b.y-a.y), 0));
        }
        public static Vector2 Point(int index, int count) => Point(index,count,BeachGeometry.BossMode);
        public static Vector2 Point(int index, int count, bool boss)
        {
            var field=BoundsFor(boss);
            int columns=Mathf.CeilToInt(Mathf.Sqrt(count*field.size.x/field.size.y));
            int rows=Mathf.CeilToInt(count/(float)columns);
            Vector2 p=new Vector2(
                Mathf.Lerp(field.min.x,field.max.x,(index%columns+.15f+CosmosSequence.Hash(index+73)*.7f)/columns),
                Mathf.Lerp(field.min.y,field.max.y,(index/columns+.15f+CosmosSequence.Hash(index+217)*.7f)/rows));
            // Preserve each cell's distribution while drawing shore cells back onto the sand.
            for(int i=0;i<20 && !BeachGeometry.Walkable(p,.12f,boss);i++)
                p=Vector2.Lerp(p,field.center,.085f);
            return p;
        }
    }
}
