Shader "Octopus/WaterBalloonFlow"
{
    Properties
    {
        _MainTex("Water pixel sphere",2D)="white"{}
        _BurstTex("Thin water membrane",2D)="white"{}
        _Clock("Actor age",Float)=0 _Phase("Phase",Float)=0 _Mode("Mode",Float)=0 _Flight("In flight",Float)=0
    }
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
            sampler2D _MainTex,_BurstTex;
            float _Clock,_Phase,_Mode,_Flight;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float hash(float x) { return frac(sin(x*127.1+17.9)*43758.5453); }
            // Idle and flight share the exact same body palette.
            float3 WaterPalette(float band)
            {
                float3 color=float3(.12,.48,.66);
                color=lerp(color,float3(.20,.64,.80),step(.24,band));
                color=lerp(color,float3(.36,.77,.88),step(.47,band));
                return lerp(color,float3(.59,.87,.93),step(.72,band));
            }
            // A swept crescent of water, not a spherical projectile with an attached ribbon.
            float4 FlyingWater(float2 p,float t)
            {
                float clock=t*10+_Phase;
                float nearest=100,along=0,cross=0;
                float2 tail=float2(-1.15+.08*sin(clock-1.1),-1+.10*sin(clock-2.0));
                for(int j=0;j<=24;j++)
                {
                    float u=j/24.0,one=1-u;
                    float2 a=tail;
                    float2 b=float2(-1.15,1.35+.08*sin(clock));
                    float2 c=float2(.68,.32+.07*sin(clock+1.4));
                    float2 center=one*one*a+2*one*u*b+u*u*c;
                    center.y+=sin(u*8-clock)*.075*sin(u*3.14159);
                    float2 tangent=normalize(2*one*(b-a)+2*u*(c-b));
                    float2 normal=float2(-tangent.y,tangent.x);
                    // Keep a round, balloon-sized leading mass; taper only its trailing surface.
                    float width=.018+.90*pow(u,2.5);
                    width*=1+.035*sin(u*10-clock);
                    float2 delta=p-center;
                    float angle=atan2(delta.y,delta.x);
                    float ripple=1+u*(.08*sin(angle*3-clock*.8)+.04*cos(angle*5+clock));
                    float distance=length(delta)/(width*ripple);
                    if(distance<nearest)
                    {nearest=distance;along=u;cross=dot(delta,normal)/width;}
                }
                // Fine inner prong sweeps out from the same tip, leaving a clear concave cutout.
                float fork=100;
                for(int k=0;k<=12;k++)
                {
                    float u=k/12.0,one=1-u;
                    float2 center=one*one*tail
                        +2*one*u*float2(-.94,-.10)+u*u*float2(-.36,-.12+.05*sin(clock));
                    fork=min(fork,length(p-center)/(.012+.085*sin(u*2.1)));
                }
                float coverage=step(min(nearest,fork),1);
                // The head retains idle cyan and translucency; white is only a broken surface glint.
                float colorClock=t*5.6+_Phase;
                float2 q=p-float2(.30,.24);
                float flow=sin(q.x*4.2+q.y*2.8-colorClock*.82)
                    +.55*sin(q.y*6-q.x*2+colorClock*1.17);
                float band=saturate(.48-q.x*.20+q.y*.13+flow*.13);
                float3 color=WaterPalette(band);
                color=lerp(color,float3(.12,.45,.62),step(.90,nearest)*.72);
                float glint=step(.74,nearest)*step(nearest,.89)*step(.22,cross)
                    *step(.32,sin(along*13-colorClock*.55));
                color=lerp(color,float3(.80,.96,.96),glint*.83);
                float moving=step(.95,sin(q.x*5+q.y*3-colorClock*.9))*step(nearest,.72);
                color=lerp(color,float3(.65,.90,.94),moving*.42);
                if(fork<nearest)color=lerp(float3(.20,.64,.80),float3(.43,.80,.90),step(fork,.55));
                float alpha=coverage*(.80+step(.87,nearest)*.12);
                // Detached flecks follow the crest rather than orbiting like idle satellites.
                for(int d=0;d<3;d++)
                {
                    float f=frac(t*1.7+d*.33+_Phase*.1);
                    float2 center=float2(-.8+f*1.45,.95+.23*sin(f*3.14159)+d*.035);
                    float2 delta=p-center;
                    delta.x+=delta.y*.55;
                    float drop=step(length(delta*float2(.8,1.5)),.055+d*.012);
                    color=lerp(color,float3(.43,.80,.90),drop);
                    alpha=max(alpha,drop*.9);
                }
                if(alpha<.01)discard;
                return float4(color,alpha);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float t=_Clock;
                if(_Mode>1.5)
                {
                    float u=i.uv.x,v=i.uv.y;
                    float wave=v+.19*sin(u*11-t*16)*u;
                    float width=.9-.45*u+.12*sin(u*15-t*13);
                    float alpha=step(abs(wave),width)*pow(saturate(1-u),.5)*.72;
                    float flow=step(.45,sin(u*17-t*18+v*2));
                    return float4(lerp(float3(.20,.61,.77),float3(.57,.86,.92),flow),alpha);
                }
                float2 p=floor(i.uv*22+.5)/22;
                if(_Mode<.5 && _Flight>.5)return FlyingWater(p,t);
                if(_Mode<.5)
                {
                    // Travelling capillary waves: the bulges circulate around the surface,
                    // instead of the whole sphere breathing in and out on one axis.
                    float clock=t*5.6+_Phase;
                    float2 v=p;
                    v.x=(v.x+.12*_Flight)/(1+.24*_Flight);
                    v.y*=1+.13*_Flight;
                    float theta=atan2(v.y,v.x);
                    float boundary=1+.115*sin(theta*3-clock)
                        +.065*sin(theta*5+clock*1.31+1.7)
                        +.045*cos(theta*2-clock*.73);
                    // A blunt leading mass with a soft, wandering trailing lobe.
                    boundary+=_Flight*.12*cos(theta);
                    float2 q=v/boundary;
                    float r=length(q);
                    float inside=step(r,1);
                    float flow=sin(q.x*4.2+q.y*2.8-clock*.82)
                        +.55*sin(q.y*6-q.x*2+clock*1.17);
                    float band=saturate(.48-q.x*.20+q.y*.13+flow*.13);
                    float3 color=WaterPalette(band);
                    // A restrained dark rim and broken travelling reflection, no glossy orb hotspot.
                    color=lerp(color,float3(.12,.45,.62),step(.90,r)*.72);
                    float reflection=step(.74,r)*step(r,.89)*step(.32,sin(theta*3+clock*.55))
                        *step(.12,-q.x+q.y*.65);
                    color=lerp(color,float3(.80,.96,.96),reflection*.83);
                    float innerArc=step(.95,sin(q.x*5+q.y*3-clock*.9))*step(r,.72);
                    color=lerp(color,float3(.65,.90,.94),innerArc*.42);
                    float alpha=inside*(.80+step(.87,r)*.12);
                    // Two small surface parcels orbit, stretch away, and join the main mass.
                    for(int k=0;k<2;k++)
                    {
                        float a=clock*.52+k*3.5;
                        float separation=.93+.22*sin(clock*.83+k*2.7);
                        float2 c=float2(cos(a),sin(a))*separation;
                        c.x=c.x*(1+.24*_Flight)-.12*_Flight;
                        float dr=length((p-c)*float2(1,1.15));
                        float parcel=step(dr,.105+.018*sin(clock+k));
                        color=lerp(color,float3(.43,.80,.90),parcel);
                        alpha=max(alpha,parcel*.88);
                    }
                    alpha*=smoothstep(0,.10,t);
                    if(alpha<.01)discard;
                    return float4(color,alpha);
                }
                // Landing: squash -> thin spreading film -> irregular rupture and falling droplets.
                float age=t;
                float fade=1-saturate((age-.22)/.7);
                float growth=.18+.88*(1-pow(1-saturate((age-.035)/.36),3));
                float2 q=float2(p.x,p.y/.58)/max(.08,growth);
                float r=length(q),theta=atan2(q.y,q.x);
                float front=1+.055*sin(theta*7-age*17+_Phase)+.035*sin(theta*13+age*22);
                float flow=sin(q.x*12+q.y*8-age*19)*sin(q.x*7-q.y*14+age*13);
                float tears=sin(q.x*14+age*7)*cos(q.y*16-age*9);
                float opening=saturate((age-.21)/.55);
                float membrane=step(r,front)*step(tears,.85-opening*.9)*saturate((front-r)*9)*fade;
                float ridge=step(.92,sin(r*22-age*13+flow*.9))*step(.1,flow);
                float3 color=lerp(float3(.22,.65,.79),float3(.66,.90,.94),ridge);
                float alpha=membrane*(.15+.12*(flow*.5+.5)+ridge*.2);
                // Fragment the expanding edge into curved sheets before it becomes droplets.
                float edge=1-smoothstep(.025,.075,abs(r-front));
                float arcs=step(-.22,sin(theta*5+age*9+_Phase));
                alpha=max(alpha,edge*arcs*fade*.67*step(.035,age));
                color=lerp(color,float3(.57,.86,.91),edge*.65);
                for(int j=0;j<20;j++)
                {
                    float h=hash(j+_Phase*31),angle=h*6.28318;
                    float life=.32+hash(j+23)*.3,local=age-.01-h*.06;
                    if(local<0 || local>life) continue;
                    float speed=.9+hash(j+41)*1.45;
                    float2 center=float2(cos(angle)*speed*local,sin(angle)*speed*local*.52+1.25*local-2.75*local*local);
                    float radius=(.027+hash(j+71)*.025)*(1-local/life*.45);
                    float drop=1-smoothstep(radius*.65,radius,length((p-center)*float2(1,.78)));
                    color=lerp(color,float3(.60,.88,.93),drop);
                    alpha=max(alpha,drop*(.85-local/life*.45));
                }
                return float4(color,alpha);
            }
            ENDHLSL
        }
    }
}
