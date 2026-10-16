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
            fixed4 frag(v2f i):SV_Target
            {
                float t=_Clock;
                if(_Mode>1.5)
                {
                    float u=i.uv.x,v=i.uv.y;
                    float alpha=(1-smoothstep(.60,1,abs(v)))*pow(saturate(1-u),.38)*.78;
                    float flow=sin(u*17-t*11+v*2);
                    return float4(lerp(float3(.12,.58,.88),float3(.75,.94,1),saturate(flow*.5+.5)),alpha);
                }
                float2 p=floor(i.uv*22+.5)/22;
                if(_Mode<.5)
                {
                    float clock=t*1.5707963+_Phase;
                    float squeeze=1+.105*sin(clock*2)+_Flight*.10;
                    float2 v=float2(p.x/squeeze,p.y*squeeze);
                    float theta=atan2(v.y,v.x),rot=.24*sin(clock);
                    float a=.17*sin(clock*2+.4),b=.055*sin(clock*3+.8);
                    float boundary=(1+a*cos(2*(theta-rot))+b*cos(3*theta+clock))/sqrt(1+.5*(a*a+b*b));
                    float2 q=v/boundary;
                    float r=length(q); if(r>.99) discard;
                    float2 uv=q+float2(.045*sin(q.y*4-clock*2),.045*sin(q.x*4+clock*3));
                    float4 tex=tex2D(_MainTex,uv*.465+.5);
                    float depth=sqrt(saturate(1-r*r));
                    float3 normal=normalize(float3(q.x+.11*sin(q.y*4-clock*2),q.y+.10*sin(q.x*5+clock*3),depth+.08*sin(q.x*3+q.y*4-clock*2)));
                    float lighting=saturate(dot(normal,normalize(float3(-.40,.53,.75))));
                    float rim=pow(saturate(r),8);
                    float shine=saturate(pow(lighting,18)*.85+rim*saturate(-q.x*.45+q.y*.7+.22)*.62);
                    float3 color=tex.rgb*.85+float3(.196,.686,.945)*.15;
                    color=lerp(color,float3(.969,.996,1),shine*.94);
                    color+=saturate(sin(q.x*5+q.y*3-clock*2)-.86)*depth*.26;
                    // Preserve the hard pixel clusters of the approved sprite rather than smooth glossy 3D shading.
                    color=floor(saturate(color)*32)/32;
                    float alpha=min(tex.a,.93+rim*.04+shine*.03);
                    alpha*=smoothstep(0,.12,t);
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
                float3 color=lerp(float3(.28,.70,.93),float3(.78,.96,1),ridge);
                float alpha=membrane*(.15+.12*(flow*.5+.5)+ridge*.2);
                float2 spriteUV=float2(p.x/(max(growth,.05)*2.1),p.y/(max(growth,.05)*1.45))+.5;
                float4 art=tex2D(_BurstTex,spriteUV);
                float artAlpha=art.a*.22*(1-saturate((age-.035)/.24))*step(.035,age);
                color=lerp(color,art.rgb,saturate(artAlpha*3)); alpha=max(alpha,artAlpha);
                for(int j=0;j<20;j++)
                {
                    float h=hash(j+_Phase*31),angle=h*6.28318;
                    float life=.32+hash(j+23)*.3,local=age-.01-h*.06;
                    if(local<0 || local>life) continue;
                    float speed=.9+hash(j+41)*1.45;
                    float2 center=float2(cos(angle)*speed*local,sin(angle)*speed*local*.52+1.25*local-2.75*local*local);
                    float radius=(.027+hash(j+71)*.025)*(1-local/life*.45);
                    float drop=1-smoothstep(radius*.65,radius,length((p-center)*float2(1,.78)));
                    color=lerp(color,float3(.68,.92,1),drop);
                    alpha=max(alpha,drop*(.85-local/life*.45));
                }
                return float4(color,alpha);
            }
            ENDHLSL
        }
    }
}
