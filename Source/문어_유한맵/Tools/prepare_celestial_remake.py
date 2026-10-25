from pathlib import Path
from PIL import Image, ImageOps, ImageEnhance, ImageDraw
import json, shutil, math, random
root=Path(__file__).resolve().parents[1]; out=root/'Art/Celestial'; frames_root=out/'frames'
cos=Image.open(out/'remake_cosmos_source.png').convert('RGBA')
yin=Image.open(out/'remake_yinyang_source.png').convert('RGBA')
# Each source is an 8 by 8 generation layout; trim gutters and remove glow haze.
def extract(atlas,row,col):
    cw=atlas.width/8; ch=atlas.height/8
    x0=round(col*cw+cw*.09); x1=round((col+1)*cw-cw*.09)
    y0=round(row*ch+ch*.09); y1=round((row+1)*ch-ch*.09)
    tile=atlas.crop((x0,y0,x1,y1))
    # Binary alpha and a 48x48 source pixel grid make the final doubled pixels sharp.
    tile=ImageOps.contain(tile,(104,104),Image.Resampling.LANCZOS)
    alpha=tile.getchannel('A')
    rgb=ImageEnhance.Contrast(tile.convert('RGB')).enhance(1.32).convert('RGBA');rgb.putalpha(alpha)
    tile=rgb
    alpha=tile.getchannel('A').point(lambda a:255 if a>=112 else 0)
    tile.putalpha(alpha)
    tile=ImageOps.contain(tile,(48,48),Image.Resampling.NEAREST)
    rgb=tile.convert('RGB').quantize(colors=20,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB')
    rgb.putalpha(tile.getchannel('A'))
    outtile=Image.new('RGBA',(64,64));outtile.alpha_composite(rgb,((64-rgb.width)//2,(64-rgb.height)//2))
    outtile=outtile.resize((128,128),Image.Resampling.NEAREST)
    canvas=Image.new('RGBA',(128,128));canvas.alpha_composite(outtile)
    return canvas
# Final game-sheet rows directly reflect animation states and retain independent readable components.
out_dir=out

def make_shockwave(frame):
    from PIL import ImageDraw
    n=64;im=Image.new('RGBA',(n,n));d=ImageDraw.Draw(im);cx=cy=31
    t=frame/7;radius=8+22*t;fade=1-t*.88
    palette=[(250,248,243),(197,198,211),(35,35,49),(7,7,14),(128,105,177)]
    def color(c):return tuple(c[:3])+(max(0,int(255*fade)),)
    for layer,offset in [(0,0),(1,2.4),(2,-2)]:
        r=max(3,radius+offset)
        for seg in range(22):
            if (seg+frame+layer)%5 in (0,): continue
            start=seg*(math.tau/22)+frame*.09+layer*.07
            end=start+math.tau/22*(.45+.12*((seg*7+frame)%4))
            pts=[]
            steps=max(2,int((end-start)*r*1.5))
            for j in range(steps+1):
                a=start+(end-start)*j/steps;rr=r+math.sin(a*5+frame)*1.2
                pts.append((round(cx+math.cos(a)*rr),round(cy+math.sin(a)*rr)))
            col=palette[(seg+layer+frame)%len(palette)]
            d.line(pts,fill=color(col),width=1 if layer else 2)
    # Radial ink shards and white spears make the ring read as an impact, not a hollow circle icon.
    for i in range(19):
        a=math.tau*i/19+frame*.11;inner=radius*.66;outer=min(31,radius+3+(i*7+frame)%6)
        p0=(round(cx+math.cos(a)*inner),round(cy+math.sin(a)*inner))
        p1=(round(cx+math.cos(a)*outer),round(cy+math.sin(a)*outer))
        d.line([p0,p1],fill=color(palette[(i+frame)%5]),width=1+(i%6==0))
    rng=random.Random(4900+frame)
    for i in range(44):
        a=rng.random()*math.tau;dist=radius+rng.randint(-6,9);x=round(cx+math.cos(a)*dist);y=round(cy+math.sin(a)*dist)
        if 1<x<n-1 and 1<y<n-1:
            size=1 if rng.random()<.78 else 2;col=palette[rng.randrange(len(palette))]
            d.rectangle((x,y,x+size,y+size),fill=color(col))
    out=im.resize((128,128),Image.Resampling.NEAREST)
    return out

row_sources=[(cos,0),(cos,1),(cos,3),(cos,4),(cos,5),(yin,0),(yin,1),(yin,2),(yin,3),(yin,4),(yin,5)]
names=['cosmos_spawn','cosmos_twinkle','cosmos_meteor','cosmos_trail','cosmos_impact','yin_energy','yang_energy','yin_yang_orbit','yin_yang_collision','yin_yang_burst','yin_yang_shockwave']
all_frames=[]
for i,(atlas,sourcerow) in enumerate(row_sources):
    row=[]
    for f in range(8):
        sprite=make_shockwave(f) if i==10 else extract(atlas,sourcerow,f)
        # Meteor head leads right, its broken tail trails to the left as in the source.
        if i in (2,3): sprite=ImageOps.mirror(sprite)
        folder=frames_root/names[i];folder.mkdir(parents=True,exist_ok=True);sprite.save(folder/f'{f:02}.png')
        row.append(sprite)
    sheet=Image.new('RGBA',(1024,128))
    for f,sprite in enumerate(row):sheet.alpha_composite(sprite,(f*128,0))
    sheet.save(out/(names[i]+'.png'))
    preview=[]
    for sprite in row:
        bg=Image.new('RGBA',(256,256),(14,17,30,255));bg.alpha_composite(sprite.resize((256,256),Image.Resampling.NEAREST));preview.append(bg.convert('RGB'))
    preview[0].save(out/(names[i]+'.gif'),save_all=True,append_images=preview[1:],duration=90,loop=0,disposal=2)
    all_frames.append(row)
atlas=Image.new('RGBA',(1024,1408))
for r,row in enumerate(all_frames):
    for f,sprite in enumerate(row):atlas.alpha_composite(sprite,(f*128,r*128))
atlas.save(out/'celestial_atlas.png')
(root/'Assets/Resources/Art/CelestialAtlas.bytes').write_bytes((out/'celestial_atlas.png').read_bytes())
for label,lo,hi in [('cosmos_sheet',0,5),('yin_yang_sheet',5,11)]:
    panel=Image.new('RGBA',(1024,(hi-lo)*128),(14,17,30,255));panel.alpha_composite(atlas.crop((0,lo*128,1024,hi*128)));panel.convert('RGB').save(out/(label+'.png'))
# Direct source comparison: source effect crops paired with their native 128px pixel recreation.
ref=Image.open(out/'reference.png').convert('RGB')
board=Image.new('RGB',(1120,640),(14,17,30));d=ImageDraw.Draw(board);d.text((12,8),'ORIGINAL REFERENCE',fill='white');d.text((790,8),'PIXEL REBUILD',fill='white')
board.paste(ref.resize((740,494)),(0,36))
for j,r in enumerate([1,3,4]):
    sprite=all_frames[r][3].resize((116,116),Image.Resampling.NEAREST);board.paste(sprite,(780+(j%2)*146,90+(j//2)*135),sprite)
for j,r in enumerate([5,7,9]):
    sprite=all_frames[r][3].resize((116,116),Image.Resampling.NEAREST);board.paste(sprite,(780+(j%2)*146,360+(j//2)*135),sprite)
board.save(out/'comparison.png')
# Static previews from the final runtime frames.
def preview(kind,rows,frames=36):
    import random
    from PIL import ImageDraw
    seq=[];rng=random.Random(19)
    for k in range(frames):
        bg=Image.new('RGBA',(512,512),(16,19,34,255));draw=ImageDraw.Draw(bg)
        for x in range(0,512,32):draw.line((x,0,x,512),fill=(24,27,44));draw.line((0,x,512,x),fill=(24,27,44))
        t=k/10
        if kind=='cosmos':
            for n in range(36):
                x=rng.Random if False else (n*83+40)%480;y=(n*137+28)%480;delay=n*.045
                if t<delay: continue
                if t<delay+.48: r,f=0,min(7,int((t-delay)*16))
                elif t<delay+.85:r,f=1,int(t*11+n)%8
                elif t<delay+1.5:
                    u=min(1,(t-delay-.85)/.5);tx=80+(n%5)*80;ty=95+(n%4)*84
                    px=x+(tx-x)*u;py=y+(ty-y)*u
                    sprite=rows[2][(k+n)%8].resize((64,64),Image.Resampling.NEAREST).rotate(-35,Image.Resampling.NEAREST,expand=True)
                    bg.alpha_composite(sprite,(int(px-sprite.width/2),int(py-sprite.height/2)));continue
                else:r,f=4,min(7,int((t-delay-1.5)*12))
                sprite=rows[r][f].resize((64,64),Image.Resampling.NEAREST);bg.alpha_composite(sprite,(x-32,y-32))
        else:
            if t<2.8:
                u=t/2.8;radius=114*(1-u*u);angle=t*2.5+t*t*1.5
                ring=rows[7][0].resize((max(24,int(radius*2.55)),)*2,Image.Resampling.NEAREST).rotate(angle*57.3,Image.Resampling.NEAREST)
                bg.alpha_composite(ring,(256-ring.width//2,256-ring.height//2))
                for side,r in [(0,5),(math.pi,6)]:
                    x=256+math.cos(angle+side)*radius;y=256+math.sin(angle+side)*radius
                    orb=rows[r][(k*3)%8].resize((64,64),Image.Resampling.NEAREST);bg.alpha_composite(orb,(int(x-32),int(y-32)))
            elif t<3.02:
                orb=rows[8][min(7,int((t-2.8)/.22*8))].resize((220,220),Image.Resampling.NEAREST);bg.alpha_composite(orb,(146,146))
            elif t<3.92:
                for r in (9,10):
                    sz=int((max(0,t-3.02)/.9)*448+16);fx=rows[r][min(7,int((t-3.02)/.9*8))].resize((sz,sz),Image.Resampling.NEAREST);bg.alpha_composite(fx,(256-sz//2,256-sz//2))
        seq.append(bg.convert('RGB'))
    seq[0].save(out/(kind+'_preview.gif'),save_all=True,append_images=seq[1:],duration=90,loop=0,disposal=2)
preview('cosmos',all_frames);preview('yin_yang',all_frames)
print('Rebuilt 88 real pixel-grid sprites from two separately composed atlases.')
