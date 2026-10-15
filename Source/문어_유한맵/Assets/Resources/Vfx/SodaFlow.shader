Shader "Octopus/SodaFlow"
{
    Properties { _Clock("Cast age",Float)=0 _Variant("Foam style",Float)=0 _Pixel("Pixel style",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Clock,_Variant,_Pixel;
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 flow:TEXCOORD1;float4 color:COLOR;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;float2 flow:TEXCOORD2;float4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.flow=v.flow;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float t=_Clock;float2 p=i.world;
                if(_Pixel>.5)p=floor(p*44)/44;
                float density=_Variant<.5?10:(_Variant<1.5?5.5:(_Variant<2.5?8:(_Variant<3.5?8.5:7)));
                float2 grid=(p+i.flow*t*.10)*density;
                float2 id=floor(grid),f=frac(grid);
                float nearest=10,second=10,chosen=0;float2 delta=0;
                for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                {
                    float2 cell=id+float2(x,y);float h=hash(cell);
                    float2 center=float2(x,y)+.5+float2(sin(h*37),cos(h*53))*.22;
                    center+=float2(sin(t*2+h*19),cos(t*1.7+h*23))*.045;
                    float2 d=f-center;float value=length(d);
                    if(value<nearest){second=nearest;nearest=value;chosen=h;delta=d;}else second=min(second,value);
                }
                float phase=frac(t*(.65+chosen*.35)+chosen*11),radius=.32+chosen*.11+sin(phase*3.14159)*.04;
                float bubble=1-smoothstep(radius-.015,radius+.055,nearest);
                float rim=1-smoothstep(.018,.062,abs(nearest-radius));
                float walls=1-smoothstep(.025,.09,second-nearest);
                float highlight=pow(saturate(dot(normalize(delta+float2(.0001,0)),normalize(float2(-.6,.8)))),5)*rim;
                float pop=smoothstep(.82,.88,phase)*(1-smoothstep(.90,1,phase));
                float shell=1-smoothstep(.02,.065,abs(nearest-(radius+(phase-.82)*1.0)));
                float coverage=max(bubble*.62,max(rim*.95,walls*.58));
                coverage=lerp(coverage,max(shell*.75,walls*.45),pop);
                float3 pearl=lerp(float3(.24,.74,.88),float3(.87,.99,1),saturate(bubble*.78+rim*.4));
                pearl=lerp(pearl,float3(.98,1,1),saturate(highlight*.95+walls*.35));
                if(_Variant>1.5 && _Variant<2.5){coverage=max(coverage,walls*.9);pearl=lerp(pearl,float3(.94,1,1),.26);}
                if(_Variant>3.5){coverage*=.82;pearl=lerp(pearl,float3(.62,.94,1),.22);}
                float radial=length(i.uv),lobes=.86+.06*sin(atan2(i.uv.y,i.uv.x)*5+i.color.g*31);
                float edge=1-smoothstep(lobes-.18,lobes+.02,radial);
                float alpha=coverage*edge*i.color.a;
                alpha=lerp(alpha,edge*i.color.a*(.55+coverage*.45),i.color.r);
                if(_Pixel>.5)pearl=floor(pearl*20)/20;
                return float4(pearl,alpha);
            }
            ENDHLSL
        }
    }
}
