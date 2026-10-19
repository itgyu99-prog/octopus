"""Offline boss animation authoring. Does not import or launch Unity.

Requires Pillow. Key poses are resampled as volume-preserving curved ribbons;
the generated body art is separate from these procedural animation drawings.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math, random, json, argparse

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/검증/giant-octopus-boss'
FPS = 25
W,H = 960,600
INK='#29212a'; PINK='#ed8eaa'; SHADE='#cf668a'; LIGHT='#ffc2cf'
FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
SMALL = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',13)
ROOTS=[(185,255),(165,370),(185,485),(775,255),(795,370),(775,485)]
AREA=(240,220,720,515)
random.seed(191026)

def lerp(a,b,t): return a+(b-a)*t
def smooth(t): return max(0,min(1,t))**2*(3-2*max(0,min(1,t)))
def mix(a,b,t): return tuple(lerp(x,y,t) for x,y in zip(a,b))
def ell(d,box,fill,outline=None,width=1): d.ellipse(tuple(map(round,box)),fill,outline,width)

class ScaledDraw:
    """Local supersampling keeps curved outlines clean without stretching sprites."""
    def __init__(self,image,origin):self.d=ImageDraw.Draw(image);self.origin=origin
    def pts(self,points):return [((x-self.origin[0])*2,(y-self.origin[1])*2) for x,y in points]
    def polygon(self,points,fill):self.d.polygon(self.pts(points),fill=fill)
    def line(self,points,fill,width=1,joint=None):self.d.line(self.pts(points),fill=fill,width=width*2,joint=joint)
    def ellipse(self,box,fill,outline=None,width=1):
        a,b=self.pts([(box[0],box[1]),(box[2],box[3])]);self.d.ellipse((*a,*b),fill,outline,width*2)

def curve(p,n=64):
    return [tuple((1-t)**3*p[0][k]+3*(1-t)**2*t*p[1][k]+3*(1-t)*t*t*p[2][k]+t**3*p[3][k] for k in (0,1)) for t in (j/(n-1) for j in range(n))]

def pose(root,side,kind,target=None):
    x,y=root; s=side
    if kind=='idle': return [(x,y+14),(x-14*s,y-65),(x+74*s,y-134),(x+44*s,y-95)]
    if kind=='coil': return [(x,y+14),(x-46*s,y-28),(x-28*s,y-104),(x+4*s,y-68)]
    if kind=='lift': return [(x,y+14),(x-32*s,y-78),(x-60*s,y-196),(x+18*s,y-176)]
    tx,ty=target
    if kind=='approach': return [(x,y+14),(x+20*s,y-90),(tx-50*s,ty-130),(tx-10*s,ty-65)]
    if kind=='contact': return [(x,y+14),(x+20*s,y-62),(tx-95*s,ty+5),(tx,ty)]
    if kind=='rebound': return [(x,y+14),(x+10*s,y-65),(tx-75*s,ty-65),(tx-22*s,ty-46)]
    raise ValueError(kind)

def interpolate(keys,t):
    def velocity(k,j,axis):
        if k==0 or k==len(keys)-1:return 0
        prev,cur,nex=keys[k-1][1][j][axis],keys[k][1][j][axis],keys[k+1][1][j][axis]
        if abs(cur-prev)<1e-8 or abs(nex-cur)<1e-8:return 0
        return .65*(nex-prev)/(keys[k+1][0]-keys[k-1][0])
    for i in range(len(keys)-1):
        ta,a=keys[i]; tb,b=keys[i+1]
        if t<=tb:
            u=max(0,min(1,(t-ta)/(tb-ta)));dt=tb-ta
            h0=2*u**3-3*u*u+1;h1=u**3-2*u*u+u;h2=-2*u**3+3*u*u;h3=u**3-u*u
            return [tuple(h0*x[k]+h1*dt*velocity(i,j,k)+h2*y[k]+h3*dt*velocity(i+1,j,k) for k in (0,1)) for j,(x,y) in enumerate(zip(a,b))]
    return keys[-1][1]

def strike(root,side,target,age,duration=1.16):
    u=age/duration
    ks=[(0,pose(root,side,'idle')),(.20,pose(root,side,'coil')),
        (.48,pose(root,side,'lift')),(.61,pose(root,side,'approach',target)),
        (.68,pose(root,side,'contact',target)),(.74,pose(root,side,'contact',target)),
        (.84,pose(root,side,'rebound',target)),(1,pose(root,side,'idle'))]
    return interpolate(ks,u)

def vertical_slam(root,side,target,age,duration=.48):
    """A fixed socket winds up, lifts, and drives a broad tip straight down."""
    x,y=root; tx,ty=target; s=side; u=max(0,min(1,age/duration))
    idle=pose(root,side,'idle')
    coil=[(x,y+14),(x-22*s,y-24),(x+8*s,y-92),(x+24*s,y-76)]
    raised=[(x,y+14),(x+10*s,y-70),(tx-8*s,ty-148),(tx,ty-118)]
    falling=[(x,y+14),(x+8*s,y-28),(tx-5*s,ty-40),(tx,ty)]
    rebound=[(x,y+14),(x+8*s,y-36),(tx-15*s,ty-68),(tx-6*s,ty-46)]
    keys=[(0,idle),(.18,coil),(.37,raised),(.56,falling),(.67,falling),(.80,rebound),(1,idle)]
    return interpolate(keys,u)

def arm(im,p,visibility=1,flat=0,ground_y=None):
    """Draw a new outline each frame; never stretch a flattened sprite."""
    pts=curve(p); left=[]; right=[]; normals=[]
    x0=math.floor(min(x for x,y in pts)-42);y0=math.floor(min(y for x,y in pts)-42)
    bw=math.ceil(max(x for x,y in pts)+42-x0);bh=math.ceil(max(y for x,y in pts)+42-y0)
    layer=Image.new('RGBA',(bw*2,bh*2));d=ScaledDraw(layer,(x0,y0))
    for j,(x,y) in enumerate(pts):
        prev=pts[max(0,j-1)]; nex=pts[min(len(pts)-1,j+1)]
        dx,dy=nex[0]-prev[0],nex[1]-prev[1]; l=max(.01,math.hypot(dx,dy))
        nx,ny=-dy/l,dx/l; normals.append((nx,ny))
        u=j/(len(pts)-1)
        r=19-7*u+5*math.exp(-((u-.88)/.13)**2)
        left.append((x+nx*r,y+ny*r)); right.append((x-nx*r,y-ny*r))
    nx,ny=normals[-1];tx,ty=ny,-nx;ex,ey=pts[-1]
    cap=[(ex+13*(nx*math.cos(a)+tx*math.sin(a)),ey+13*(ny*math.cos(a)+ty*math.sin(a))) for a in (math.pi*j/16 for j in range(17))]
    outline=left+cap+right[::-1]
    d.polygon(outline,fill=PINK)
    d.polygon(pts+right[::-1],fill=SHADE)
    d.line(outline+[outline[0]],fill=INK,width=4,joint='curve')
    # Suckers use constant arc-length spacing and constant radii.
    distance=0; last=-16
    for j in range(1,len(pts)-3):
        distance+=math.dist(pts[j-1],pts[j])
        if distance-last>=20:
            x,y=pts[j]; nx,ny=normals[j]; x+=nx*7; y+=ny*7
            ell(d,(x-5,y-3.8,x+5,y+3.8),LIGHT,INK,1); last=distance
    if flat:
        x,y=p[-1]; ell(d,(x-39,y-14,x+39,y+15),PINK,INK,3)
        for dx in (-29,-14,1,16,29): ell(d,(x+dx-4,y-1,x+dx+4,y+5),LIGHT,INK)
    layer=layer.resize((bw,bh),Image.Resampling.LANCZOS)
    if visibility<1:
        # Occlusion plane, not alpha fading: entire curve descends below ground.
        base=p[0][1]-14 if ground_y is None else ground_y
        mask=Image.new('L',layer.size); md=ImageDraw.Draw(mask)
        if base+1-y0>=0:md.rectangle((0,0,bw,round(base+1-y0)),fill=255)
        layer.putalpha(Image.composite(layer.getchannel('A'),Image.new('L',layer.size),mask))
    im.alpha_composite(layer,(x0,y0))

def hole(im,r,front=False,activity=0):
    x,y=r; d=ImageDraw.Draw(im)
    if not front:
        # Recess, sand bevel, then the dark opening. The arm is drawn over this
        # layer and the near lip is restored afterward to establish depth.
        ell(d,(x-47,y-15,x+47,y+19),'#76523e')
        ell(d,(x-43,y-15,x+43,y+17),'#bd925d','#79533d',2)
        ell(d,(x-35,y-11,x+35,y+13),'#30272e','#63423b',3)
        ell(d,(x-27,y-8,x+27,y+9),'#3d3035')
        d.arc((x-40,y-14,x+40,y+16),190,350,fill='#e1b878',width=3)
    else:
        d.pieslice((x-46,y-16,x+46,y+25),0,180,fill='#b68a59',outline='#74513d',width=2)
        d.arc((x-46,y-16,x+46,y+25),0,180,fill='#edc68c',width=5)
        for dx,dy,rr in [(-37,8,5),(30,13,5),(41,3,3),(-23,16,4),(-3,19,3)]:
            ell(d,(x+dx-rr,y+dy-rr,x+dx+rr,y+dy+rr),'#caa575','#846448',1)
    if activity>0:
        # Small foreground clods rise from the lip, keeping the socket embedded
        # in the field instead of reading as a flat overlay.
        for j in range(4):
            a=j*math.tau/4+0.4; rr=31+activity*12
            xx=x+math.cos(a)*rr; yy=y+math.sin(a)*rr*.22-activity*13
            sz=3+(j%2)*2
            ell(d,(xx-sz,yy-sz*.6,xx+sz,yy+sz*.6),'#aa8054','#76543e',1)
        for j in range(7):
            a=j*2.399; rr=12+activity*26+j%3*4
            xx=x+math.cos(a)*rr; yy=y+math.sin(a)*rr*.35-activity*14
            ell(d,(xx-5,yy-3,xx+5,yy+3),'#e9c899')

def impact(im,pos,age,wide=False,under=False):
    """Press the field at contact, then throw earth around the planted tip."""
    if not 0<=age<.42:return
    x,y=pos;u=age/.42;ease=smooth(u);fade=1-u
    scale=1.0 if wide=='barrage' else .64 if wide else .42
    pad=52;ox=round(x-pad);oy=round(y-pad)
    layer=Image.new('RGBA',(pad*2,pad*2));d=ImageDraw.Draw(layer)
    x-=ox;y-=oy
    # Keep each footprint smaller than the gap between lanes, so six impacts
    # read as separate contacts rather than one continuous stripe.
    rx=(14+ease*28)*scale;ry=max(5,rx*.19)
    if under:
        # A shallow depression and short radial splits sit underneath the arm.
        d.ellipse((x-rx,y-ry,x+rx,y+ry),fill=(83,57,47,round(24*fade)),outline=(126,85,57,round(120*fade)),width=2)
        d.arc((x-rx*.72,y-ry*.7,x+rx*.72,y+ry*.8),8,172,fill=(229,190,132,round(110*fade)),width=2)
        d.ellipse((x-14,y-4,x+14,y+4),fill=(67,47,43,round(34*fade)))
        for side in (-1,1):
            d.line((x+side*14,y,x+side*(22+fade*4),y-3),fill=(94,65,51,round(130*fade)),width=2)
            d.line((x+side*22,y-3,x+side*(26+fade*5),y-8),fill=(112,77,56,round(115*fade)),width=1)
        im.alpha_composite(layer,(ox,oy));return
    # The sand plume starts beside the sucker tip so its silhouette stays clear.
    for side in (-1,1):
        spread=(14+ease*24)*scale
        for j in range(3):
            drift=spread*(.55+j*.22)
            lift=(3+j*4+ease*12)*scale
            xx=x+side*drift; yy=y-lift+math.sin(u*math.pi)*5
            size=(2+j%2+fade*1.5)*scale
            a=round((125-j*16)*fade)
            d.ellipse((xx-size*1.25,yy-size*.65,xx+size*1.25,yy+size*.65),fill=(224,190,143,a),outline=(151,112,76,round(a*.8)),width=1)
    for j in range(6):
        a=math.pi+(j/5)*math.pi
        distance=(14+ease*27)*(1+(j%2)*.12)*scale
        xx=x+math.cos(a)*distance;yy=y+math.sin(a)*(8+ease*13)*scale
        size=max(1,(2+fade*(2+j%2))*scale)
        alpha=round(145*fade)
        d.polygon([(xx-size,yy),(xx,yy-size*.7),(xx+size,yy+size*.2)],fill=(184,139,91,alpha),outline=(137,97,65,alpha))
        if wide=='rock' and j%2==0:
            d.polygon([(xx-3,yy+2),(xx+2,yy-4),(xx+5,yy+3)],fill=(162,140,137,alpha),outline=(41,33,42,alpha))
    im.alpha_composite(layer,(ox,oy))

def warning(im,box,phase):
    ov=Image.new('RGBA',im.size); d=ImageDraw.Draw(ov)
    d.rounded_rectangle(box,radius=15,fill=(228,66,81,round(12+12*phase)),outline=(206,102,95,112),width=2)
    im.alpha_composite(ov)

def rock(im,pos,r=23,angle=0):
    x,y=pos; d=ImageDraw.Draw(im)
    pts=[]
    for j in range(8):
        a=j*math.tau/8+angle; rr=r*(.88+.12*math.sin(j*6+1))
        pts.append((x+math.cos(a)*rr,y+math.sin(a)*rr))
    d.polygon(pts,fill='#a28c89');d.line(pts+[pts[0]],fill=INK,width=3,joint='curve')
    d.line((pts[1],(x-4,y+5),pts[4]),fill='#6e606b',width=3)
    d.line((pts[5],(x+5,y-9),pts[7]),fill='#d2b8a6',width=3)

def grip(im,pos,side):
    # Back arm -> rock -> front wrapped tip; shared center until release.
    x,y=pos;d=ImageDraw.Draw(im)
    back=[(x+29*math.cos(a),y-1+26*math.sin(a)) for a in (math.radians(175+j*180/32) for j in range(33))]
    d.line(back,fill=INK,width=17,joint='curve');d.line(back,fill=PINK,width=11,joint='curve')
    rock(im,pos)
    front=[(x+27*math.cos(a),y+5+23*math.sin(a)) for a in (math.radians(j*185/32) for j in range(33))]
    d.line(front,fill=INK,width=17,joint='curve');d.line(front,fill=PINK,width=11,joint='curve')
    for dx in (-17,-3,12):ell(d,(x+dx-4,y+13,x+dx+4,y+19),LIGHT,INK)

def ground_pose(root,s,depth):
    p=pose(root,s,'idle'); return [(x,y+depth) for x,y in p]

class Renderer:
    def __init__(self):
        self.field=Image.open(ROOT/'Source/문어_유한맵/Assets/Resources/Art/Beach.bytes').convert('RGBA').resize((960,450),Image.Resampling.LANCZOS)
        self.body=Image.open(OUT/'assets/body.png').convert('RGBA')
        self.body=self.body.crop(self.body.getbbox()); self.body.thumbnail((300,165),Image.Resampling.LANCZOS)
        sheet=Image.open(ROOT/'문어 에셋파일/대기모션.png').convert('RGBA')
        self.player=sheet.crop((0,0,362,362));self.player=self.player.crop(self.player.getbbox());self.player.thumbnail((30,30),Image.Resampling.LANCZOS)
    def base(self,title,sub,t):
        im=Image.new('RGBA',(W,H),'#ddbc91');im.alpha_composite(self.field,(0,150))
        d=ImageDraw.Draw(im);d.rectangle((0,0,W,59),fill='#242431');d.text((22,9),title,font=FONT,fill='#fff0dc');d.text((22,35),sub,font=SMALL,fill='#c7bec4')
        # Fixed mantle location, independent facial expression.
        im.alpha_composite(self.body,((W-self.body.width)//2,62))
        d.rounded_rectangle((350,211,610,218),radius=3,fill='#3b2b36')
        d.rounded_rectangle((353,213,607,216),radius=2,fill='#ef7c9c')
        d.rounded_rectangle((236,224,724,519),radius=45,outline='#d58e99',width=2)
        return im
    def player_at(self,im,pos):
        im.alpha_composite(self.player,(round(pos[0]-15),round(pos[1]-15)))
    def render(self,pattern,t):
        names={1:'01 · 가로 3구역 순차 내려찍기',2:'02 · 여섯 촉수 30연타',3:'03 · 바위 8개 집어 던지기',4:'04 · 잠복 후 반쪽 구역 난타'}
        subs={1:'상단 → 중앙 → 하단 · 공격이 끝난 구역은 즉시 해제',2:'각 팔 5회 · 뿌리 고정 · 접촉 간격 0.16초',3:'돌 아래 받치기 → 감아 쥐기 → 들어 올리기 → 투척 → 착탄',4:'상 · 하 · 좌 · 우 예시 / 각 방향 3초 난타'}
        im=self.base(names[pattern],subs[pattern],t); d=ImageDraw.Draw(im)
        roots=list(ROOTS); ps=[pose(r,1 if i<3 else -1,'idle') for i,r in enumerate(roots)]
        flats=[0]*6; visibility=[1]*6; effects=[]; dust=[0]*6; held=[]; rocks=[]; trails=[]
        player=(480,470)
        if pattern==1:
            for k in range(3):
                start=.6+k*1.3; age=t-start; yy=265+k*90
                if 0<=age<.79:warning(im,(242,yy-35,718,yy+35),age/.79)
                for i in (k,k+3):
                    target=(415 if i<3 else 545,yy)
                    if 0<=age<=1.16:
                        ps[i]=strike(roots[i],1 if i<3 else -1,target,age)
                        flats[i]=.68<=age/1.16<=.74
                    effects.append((target,age-.80,True))
            player=(480,lerp(300,490,smooth(t/3.2))) if t<3.7 else (480,lerp(490,350,smooth((t-3.7)/1.2)))
        elif pattern==2:
            count=0
            for e in EVENTS30:
                i=e['arm']; age=t-e['start']; target=e['target']
                if 0<=age<.46:
                    x,y=target;warning(im,(x-24,y-12,x+24,y+12),age/.46)
                if 0<=age<.92:
                    ps[i]=strike(roots[i],1 if i<3 else -1,target,age,.92)
                    flats[i]=.68<=age/.92<=.74
                hit=e['contact'];effects.append((target,t-hit,False))
                if t>=hit:count+=1
            d.text((785,15),f'{count:02d} / 30',font=FONT,fill='#ffccdc')
            for i in range(6):
                n=sum(t>=e['contact'] for e in EVENTS30 if e['arm']==i)
                x,y=roots[i];d.text((x-20,y+25),f'{i+1} · {n}/5',font=SMALL,fill=INK)
            player=(480+70*math.sin(t*1.7),480)
        elif pattern==3:
            count=0
            for e in EVENTS8:
                i=e['arm']; r=roots[i]; s=1 if i<3 else -1; age=t-e['start']; release=e['release']; target=e['target']
                pickup=(r[0]+s*67,r[1]-10)
                low=[r,(r[0]+s*8,r[1]-45),(pickup[0]-s*25,pickup[1]+20),pickup]
                loaded=[r,(r[0]-s*30,r[1]-45),(r[0]-s*70,r[1]-150),(r[0]-s*30,r[1]-158)]
                cocked=[r,(r[0]-s*55,r[1]-50),(r[0]-s*85,r[1]-190),(r[0]-s*50,r[1]-180)]
                throwing=[r,(r[0]+s*30,r[1]-100),(r[0]+s*90,r[1]-135),(r[0]+s*125,r[1]-105)]
                keys=[(0,pose(r,s,'idle')),(.24,low),(.46,low),(.82,loaded),(1.08,cocked),(1.24,throwing),(1.43,pose(r,s,'approach',target)),(1.90,pose(r,s,'idle'))]
                if 0<=age<1.9:ps[i]=interpolate(keys,age)
                if 0<=age<.24:rocks.append((pickup,23,0))
                if .24<=age<1.24:held.append((ps[i][-1],s))
                flight=t-release; launch=throwing[-1]
                if 0<=flight<.88:
                    u=flight/.88; pos=mix(launch,target,u);pos=(pos[0],pos[1]-math.sin(math.pi*u)*90)
                    rocks.append((pos,23,u*4)); warning(im,(target[0]-32,target[1]-16,target[0]+32,target[1]+16),u)
                effects.append((target,flight-.88,'rock'))
                if t>=release:count+=1
            d.text((799,15),f'{count} / 8',font=FONT,fill='#ffccdc')
        elif pattern==4:
            cycle=int(t/6.6)%4; u=t%6.6
            boxes=[(240,220,720,367),(240,367,720,515),(240,220,480,515),(480,220,720,515)]
            box=boxes[cycle]; labels=['상단','하단','왼쪽','오른쪽']
            d.text((810,15),labels[cycle],font=FONT,fill='#ffccdc')
            if .25<=u<4.9:warning(im,box,min(1,(u-.25)*2))
            b0,b1,b2,b3=box
            if b2-b0 > b3-b1:
                # A broad top/bottom zone gets six distinct vertical lanes.
                emerged=[(lerp(b0+24,b2-24,i/5),b1+17) for i in range(6)]
            else:
                # A narrow left/right zone gets two columns by three rows.
                emerged=[(lerp(b0+38,b2-38,i%2),lerp(b1+30,b3-65,i//2/2)) for i in range(6)]
            for i,r in enumerate(ROOTS):
                s=1 if i<3 else -1
                if u<.8:
                    dep=180*smooth(u/.8);ps[i]=ground_pose(r,s,dep);visibility[i]=.5;dust[i]=math.sin(math.pi*min(1,u/.8))
                elif u<1.1:
                    ps[i]=ground_pose(r,s,200);visibility[i]=.5
                elif u<1.7:
                    roots[i]=emerged[i];dep=180*(1-smooth((u-1.1)/.6));ps[i]=ground_pose(roots[i],s,dep);visibility[i]=.5;dust[i]=math.sin(math.pi*(u-1.1)/.6)
                elif u<4.7:
                    roots[i]=emerged[i]
                    beat=.40; offset=i*.055; phase=u-1.7+offset; age=phase%beat; n=int(phase/beat)
                    # Every root stays planted; each arm winds up and drops into
                    # a neighboring vertical lane so the middle is struck too.
                    rx,ry=roots[i]
                    if b2-b0 > b3-b1:
                        tx=max(b0+24,min(b2-24,rx+math.sin(n*1.8+i)*9))
                        ty=b3-20
                    else:
                        tx=max(b0+28,min(b2-28,rx+math.sin(n*1.8+i)*8))
                        ty=min(b3-18,ry+72+math.cos(n+i)*8)
                    target=(tx,ty);ps[i]=vertical_slam(roots[i],s,target,age,beat)
                    if u<1.9:ps[i]=[mix(a,b,smooth((u-1.7)/.2)) for a,b in zip(pose(roots[i],s,'idle'),ps[i])]
                    if .52<age/beat<.77:trails.append(vertical_slam(roots[i],s,target,max(0,age-.035),beat))
                    flats[i]=.56<=age/beat<=.70
                    effects.append((target,age-.60*beat,'barrage'))
                elif u<5.3:
                    roots[i]=emerged[i];ps[i]=ground_pose(roots[i],s,180*smooth((u-4.7)/.6));visibility[i]=.5;dust[i]=math.sin(math.pi*(u-4.7)/.6)
                elif u<5.6:
                    ps[i]=ground_pose(r,s,200);visibility[i]=.5
                elif u<6.2:
                    ps[i]=ground_pose(r,s,180*(1-smooth((u-5.6)/.6)));visibility[i]=.5;dust[i]=math.sin(math.pi*(u-5.6)/.6)
            player=[(480,465),(480,280),(640,420),(320,420)][cycle]
        for i,r in enumerate(roots):hole(im,r,False,dust[i])
        # Ground compression is behind the limbs; flying soil is added after.
        for pos,age,wide in effects:impact(im,pos,age,wide,under=True)
        if trails:
            trail=Image.new('RGBA',im.size)
            for p in trails:arm(trail,p)
            trail.putalpha(trail.getchannel('A').point(lambda a:round(a*.20)))
            im.alpha_composite(trail)
        # Sort roots for stable ground-plane overlap.
        for i in sorted(range(6),key=lambda j:roots[j][1]):
            arm(im,ps[i],visibility[i],flats[i],roots[i][1]);hole(im,roots[i],True)
        for pos,s in held:grip(im,pos,s)
        for pos,rr,ang in rocks:rock(im,pos,rr,ang)
        for pos,age,wide in effects:impact(im,pos,age,wide)
        self.player_at(im,player)
        return im.convert('RGB')

EVENTS30=[]
for j in range(30):
    start=.5+j*.16; EVENTS30.append(dict(arm=j%6,start=round(start,4),contact=round(start+.68*.92,4),target=(random.randint(280,680),random.randint(245,490))))
EVENTS8=[]
for j in range(8):
    start=.4+j*.72;EVENTS8.append(dict(arm=j%6,start=start,release=start+1.24,target=(random.randint(290,670),random.randint(290,490))))

def save_gif(frames,path):
    # A single palette eliminates temporal background shimmer.
    sample=Image.new('RGB',(W,H*min(8,len(frames))))
    for j in range(min(8,len(frames))):sample.paste(frames[round(j*(len(frames)-1)/max(1,min(8,len(frames))-1))],(0,j*H))
    pal=sample.quantize(colors=240)
    indexed=[f.quantize(palette=pal,dither=Image.Dither.NONE) for f in frames]
    indexed[0].save(path,save_all=True,append_images=indexed[1:],duration=40,loop=0,optimize=False,disposal=1)

def sheets():
    asset=OUT/'assets';asset.mkdir(exist_ok=True)
    clips={
        'slam':(1.16,lambda t:strike((70,235),1,(280,220),t)),
        'ground':(2.4,lambda t:ground_pose((170,235),1,180*(1-smooth(t/1.2)) if t<1.2 else 180*smooth((t-1.2)/1.2))),
        'grip-throw':(1.92,None)}
    manifest={'fps':FPS,'cell_size':[384,288],'order':'row-major, left to right, top to bottom','root_pivot_px':[70,235], 'body_origin':'built-in image_gen original; animation arms are procedural drawings, not imagegen frames','clips':{},'events_30':EVENTS30,'events_8':EVENTS8}
    zoom=[]
    for name,(dur,func) in clips.items():
        n=round(dur*FPS)+1; sheet=Image.new('RGBA',(384*8,288*math.ceil(n/8)))
        for j in range(n):
            t=min(dur,j/FPS); cell=Image.new('RGBA',(W,H))
            if name=='grip-throw':
                r=(70,235);s=1;low=[r,(85,205),(120,245),(145,220)]
                load=[r,(40,150),(30,60),(65,65)]
                throw=[r,(100,140),(170,70),(210,100)]
                p=interpolate([(0,pose(r,s,'idle')),(.24,low),(.46,low),(.82,load),(1.08,load),(1.24,throw),(1.50,pose(r,s,'approach',(280,220))),(1.92,pose(r,s,'idle'))],t)
            else:p=func(t)
            activity=0
            if name=='ground':
                phase=(t if t<1.2 else t-1.2)/1.2
                activity=math.sin(math.pi*phase)
            hole(cell,(70,235) if name!='ground' else (170,235),activity=activity)
            arm(cell,p,.5 if name=='ground' else 1, name=='slam' and .68<=t/1.16<=.74,235)
            if name=='grip-throw':
                if t<.24:rock(cell,(145,220))
                elif t<1.24:grip(cell,p[-1],1)
                elif t<1.92:
                    u=(t-1.24)/.88;pt=mix(throw[-1],(300,220),u);rock(cell,(pt[0],pt[1]-math.sin(math.pi*u)*60),23,u*4)
            hole(cell,(170,235) if name=='ground' else (70,235),True,activity if name=='ground' else 0)
            crop=cell.crop((0,0,384,288));sheet.alpha_composite(crop,((j%8)*384,(j//8)*288))
            if j==0:crop.save(asset/f'{name}-first.png')
            if name=='grip-throw' and j==20:crop.save(asset/'rock-held.png')
            if name=='slam' and j==20:crop.save(asset/'slam-contact.png')
            back=Image.new('RGB',(384,288),'#dfbc8f');back.paste(crop,(0,0),crop)
            ImageDraw.Draw(back).text((10,8),name+'  '+str(j+1),font=SMALL,fill=INK);zoom.append(back)
        sheet.save(asset/f'{name}-sheet.png')
        manifest['clips'][name]={'frames':n,'columns':8,'fps':25,'cell_size':[384,288],'root_pivot_px':[170,235] if name=='ground' else [70,235]}
    # Closeup triples shown sequentially at native 25 fps, scaled 2x.
    big=[f.resize((768,576),Image.Resampling.LANCZOS) for f in zoom]
    # save_gif expects fixed frame size only for palette sampling, adapt locally.
    sample=Image.new('RGB',(768,576*3))
    for k,j in enumerate((10,45,110)):sample.paste(big[j],(0,k*576))
    palette=sample.quantize(colors=240)
    q=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in big]
    q[0].save(OUT/'05-detail.gif',save_all=True,append_images=q[1:],duration=40,loop=0,optimize=False)
    for front,name in ((False,'ground-back'),(True,'ground-front')):
        layer=Image.new('RGBA',(W,H));hole(layer,(70,235),front);layer.crop((0,0,384,288)).save(asset/f'{name}.png')
    layer=Image.new('RGBA',(96,96));rock(layer,(48,48));layer.save(asset/'rock.png')
    manifest['mirroring']={'right_arm':'flip horizontally inside a 384x288 cell','right_root_pivot_px':[314,235]}
    manifest['durations']={'01-pattern.gif':5.6,'02-pattern.gif':6.8,'03-pattern.gif':9.2,'04-pattern.gif':26.4}
    manifest['half_arena']={'order':['top','bottom','left','right'],'cycle_seconds':6.6,'barrage_seconds':3,'arm_count':6,'barrage_axis':'vertical down','per_arm_cycle_seconds':0.4,'roots_fixed_during_barrage':True,'root_changes':'only while fully occluded below ground'}
    (asset/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')

def pixel_poses_gif():
    """Review the nine generated key poses in order, without inventing inbetweens."""
    source=Image.open(OUT/'assets/tentacle-pixel-nine-poses.png').convert('RGBA')
    # Coordinates refer to the saved 1328x1184 design sheet, excluding its labels.
    assert source.size==(1328,1184), 'Recheck crop bounds if the source sheet changes'
    boxes=[(185,65,452,430),(534,85,801,430),(875,65,1142,430),
           (185,495,452,765),(534,530,801,765),(875,505,1142,765),
           (185,820,452,1145),(534,820,801,1145),(875,820,1142,1080)]
    captions=['솟기 / 기본','휘기','S곡선','웅크리기','접기 / 잠복 준비',
              '앞으로 접기','아래로 뻗기','내려찍기','회수']
    frames=[]
    for i,(box,caption) in enumerate(zip(boxes,captions)):
        frame=Image.new('RGB',(560,560),'#241923')
        sprite=source.crop(box)
        # Upright poses share their ground line; downward poses share the upper socket.
        origin=((560-sprite.width)//2,455-sprite.height if i<6 else 132)
        frame.paste(sprite,origin,sprite)
        draw=ImageDraw.Draw(frame)
        draw.text((28,22),'문어 보스 · 다리 도트 자세',font=FONT,fill='#ffd9e3')
        draw.text((28,57),f'{i+1} / 9   {caption}',font=FONT,fill='#e7bdcb')
        draw.text((28,504),'9가지 정지 자세 순차 미리보기',font=SMALL,fill='#d4b9c3')
        for j in range(9):
            x=28+j*55
            draw.rectangle((x,539,x+43,544),fill='#fa94b1' if i==j else '#59404d')
        frames.append(frame)
    palette=Image.new('RGB',(560,560*9))
    for i,frame in enumerate(frames):palette.paste(frame,(0,i*560))
    palette=palette.quantize(colors=256)
    frames=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    path=OUT/'tentacle-pixel-nine-poses.gif'
    frames[0].save(path,save_all=True,append_images=frames[1:],
                   duration=650,loop=0,optimize=False,disposal=2)
    with Image.open(path) as decoded:
        assert decoded.n_frames==9
        duration=0
        for i in range(decoded.n_frames):
            decoded.seek(i);duration+=decoded.info['duration']
        assert duration==5850
    print(f'{path}: 9 frames, 560x560, {duration} ms, infinite loop')


def asset_pixel_slam_gif():
    """Fast source-cel playback, with the requested 1.5x vertical body length."""
    source=Image.open(OUT/'assets/tentacle-pixel-user-reference.png').convert('RGBA')
    assert source.size==(1328,1184)
    boxes=[(185,65,452,430),(534,85,801,430),(875,65,1142,430),
           (185,500,452,765),(534,533,801,765),(875,515,1142,765),
           (185,829,452,1145),(534,829,801,1145),(875,829,1142,1080)]
    sprites=[source.crop(box) for box in boxes]
    long_sprites=[]
    for i,sprite in enumerate(sprites):
        # Keep the sand socket's original proportions; lengthen only the body.
        socket_height=45 if i<6 else 55
        if i<6:
            body=sprite.crop((0,0,sprite.width,sprite.height-socket_height))
            socket=sprite.crop((0,sprite.height-socket_height,sprite.width,sprite.height))
        else:
            socket=sprite.crop((0,0,sprite.width,socket_height))
            body=sprite.crop((0,socket_height,sprite.width,sprite.height))
        body=body.resize((body.width,round(body.height*1.5)),Image.Resampling.NEAREST)
        tall=Image.new('RGBA',(sprite.width,body.height+socket.height))
        if i<6:
            tall.alpha_composite(body);tall.alpha_composite(socket,(0,body.height))
        else:
            tall.alpha_composite(socket);tall.alpha_composite(body,(0,socket.height))
        long_sprites.append(tall)
    sprites=long_sprites
    sheet=Image.new('RGBA',(960,1800))
    for i,sprite in enumerate(sprites):
        x=(i%3)*320+(320-sprite.width)//2;y=(i//3)*600+580-sprite.height
        sheet.alpha_composite(sprite,(x,y))
        ImageDraw.Draw(sheet).text(((i%3)*320+150,(i//3)*600+10),str(i+1),font=FONT,fill=INK)
    sheet.save(OUT/'assets/tentacle-pixel-tall-sheet.png')
    # Preparation, extension, strike and recoil. Every source pose is used.
    order=[4,3,2,1,6,9,7,8,9,6,5]
    frames=[]
    for number in order:
        sprite=sprites[number-1]
        frame=Image.new('RGBA',(560,720),'#ecd0a7')
        frame.alpha_composite(sprite,((560-sprite.width)//2,625-sprite.height))
        frames.append(frame.convert('RGB'))
    sample=Image.new('RGB',(560,720*len(frames)))
    for i,frame in enumerate(frames):sample.paste(frame,(0,i*720))
    palette=sample.quantize(colors=256)
    quantized=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    path=OUT/'tentacle-pixel-slam.gif'
    quantized[0].save(path,save_all=True,append_images=quantized[1:],
                      duration=50,loop=0,disposal=2,optimize=False)
    with Image.open(path) as gif:
        assert gif.n_frames==len(order)
        times=[]
        for j in range(gif.n_frames):gif.seek(j);times.append(gif.info['duration'])
        assert times==[50]*len(order)
    assert max(s.height for s in sprites)<625
    print(f'Tall source assets: {order}; body height 1.5x, original width and sand; 11 cels, 50ms each, 550ms loop')


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--pattern',type=int);parser.add_argument('--sheets',action='store_true');parser.add_argument('--verify',action='store_true');parser.add_argument('--pixel-poses',action='store_true');parser.add_argument('--pixel-slam',action='store_true');args=parser.parse_args()
    OUT.mkdir(exist_ok=True,parents=True)
    if args.pixel_slam:asset_pixel_slam_gif();return
    if args.pixel_poses:pixel_poses_gif();return
    if args.verify:
        report={'gif':{},'alpha':{},'counts_per_arm':[sum(e['arm']==i for e in EVENTS30) for i in range(6)],'rock_releases':len(EVENTS8)}
        for path in sorted(OUT.glob('0*.gif')):
            gif=Image.open(path);durations=[];decoded=[]
            for k in range(gif.n_frames):
                gif.seek(k);durations.append(gif.info.get('duration',0));decoded.append(gif.convert('RGB'))
            report['gif'][path.name]={'encoded_frames':len(decoded),'duration_ms':sum(durations),'motion_step_ms':min(durations),'size':gif.size}
            if path.name[1] in '1234':
                p=int(path.name[1]);target={1:1200,2:1000,3:1440,4:2520}[p];elapsed=0;start=0
                for k,dt in enumerate(durations):
                    if elapsed>=target:start=k;break
                    elapsed+=dt
                contact=Image.new('RGB',(1920,900))
                for k in range(12):
                    idx=min(len(decoded)-1,start+k);f=decoded[idx].resize((480,300));ImageDraw.Draw(f).text((8,275),f'GIF frame {idx}',font=SMALL,fill='white');contact.paste(f,((k%4)*480,(k//4)*300))
                contact.save(OUT/f'qa-{p}-consecutive.jpg',quality=90)
            del decoded
        for path in sorted((OUT/'assets').glob('*.png')):
            im=Image.open(path);report['alpha'][path.name]=im.mode=='RGBA' and im.getchannel('A').getextrema()[0]==0
        assert report['counts_per_arm']==[5]*6 and report['rock_releases']==8
        assert all(x['motion_step_ms']==40 for x in report['gif'].values())
        assert all(report['alpha'].values())
        (OUT/'qa-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2));return
    if args.sheets:sheets();return
    r=Renderer()
    if args.pattern:
        p=args.pattern;dur={1:5.6,2:6.8,3:9.2,4:26.4}[p];frames=[]
        for j in range(round(dur*FPS)):
            frames.append(r.render(p,j/FPS))
            if j%100==0:print(f'pattern {p} {j}/{round(dur*FPS)}',flush=True)
        save_gif(frames,OUT/f'0{p}-pattern.gif')
        # Consecutive frames around actual contact, never cherry-picked isolated poses.
        start={1:30,2:27,3:36,4:63}[p];contact=Image.new('RGB',(480*4,300*3))
        for k in range(12):
            f=frames[min(len(frames)-1,start+k)].resize((480,300));ImageDraw.Draw(f).text((8,275),f'{(start+k)/25:.2f}s',font=SMALL,fill='white');contact.paste(f,((k%4)*480,(k//4)*300))
        contact.save(OUT/f'qa-{p}-consecutive.jpg',quality=90)
        print('saved',OUT/f'0{p}-pattern.gif',flush=True)
    else:
        im=r.base('거대 문어 · 전투 배치 시안','본체 고정 / 전투 촉수 6개 / 플레이어 크기 비교',0)
        for i,rt in enumerate(ROOTS):hole(im,rt);arm(im,pose(rt,1 if i<3 else -1,'idle'));hole(im,rt,True)
        r.player_at(im,(480,450));im.save(OUT/'boss-field.png')

if __name__=='__main__':main()
