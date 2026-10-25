"""Bake the symmetric yin/yang symbols and animated generated ink blast.

Run with the bundled Python runtime. PNG payloads are loaded by YinYangArt,
so Unity texture-import defaults cannot reduce their resolution or alpha.
"""
from pathlib import Path
import math, json, shutil, argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def gaussian_filter(values, radius):
    return np.array(Image.fromarray((values*255).astype('uint8')).filter(ImageFilter.GaussianBlur(radius))).astype(float)/255

def map_coordinates(values, coords, **_):
    sy,sx=coords
    ix=np.floor(sx).astype(int);iy=np.floor(sy).astype(int)
    valid=(ix>=0)&(iy>=0)&(ix<values.shape[1]-1)&(iy<values.shape[0]-1)
    fx=sx-ix;fy=sy-iy
    ix=np.clip(ix,0,values.shape[1]-2);iy=np.clip(iy,0,values.shape[0]-2)
    return ((1-fx)*(1-fy)*values[iy,ix]+fx*(1-fy)*values[iy,ix+1]+(1-fx)*fy*values[iy+1,ix]+fx*fy*values[iy+1,ix+1])*valid

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/YinYangV2'
RES = ROOT / 'Assets/Resources/Art/YinYang'
ART.mkdir(exist_ok=True, parents=True)
RES.mkdir(exist_ok=True, parents=True)

def save(image, name):
    image.save(ART / (name + '.png'))
    (RES / (name + '.bytes')).write_bytes((ART / (name + '.png')).read_bytes())

def symbols():
    n = 1024
    # Rounded droplet head with a short tucked curl. Draw as cubic curves at 4x
    # resolution so the silhouette stays clean when sampled in game.
    def cubic(p0,p1,p2,p3,steps=80):
        out=[]
        for i in range(steps):
            t=i/(steps-1); q=1-t
            out.append((q**3*p0[0]+3*q*q*t*p1[0]+3*q*t*t*p2[0]+t**3*p3[0],
                        q**3*p0[1]+3*q*q*t*p1[1]+3*q*t*t*p2[1]+t**3*p3[1]))
        return out
    path=[]
    curves=[((128,18),(182,18),(220,56),(220,104)),
            ((220,104),(220,144),(192,174),(158,183)),
            ((158,183),(143,187),(133,194),(125,207)),
            ((125,207),(119,216),(107,221),(98,216)),
            ((98,216),(109,211),(116,203),(118,194)),
            ((118,194),(121,183),(112,176),(95,170)),
            ((95,170),(57,156),(37,128),(38,93)),
            ((38,93),(40,50),(73,18),(128,18))]
    for curve in curves: path.extend(cubic(*curve,steps=48)[:-1])
    hi=Image.new('L',(n,n)); ImageDraw.Draw(hi).polygon([(round(x*4),round(y*4)) for x,y in path],fill=255)
    alpha=hi.filter(ImageFilter.GaussianBlur(.65))
    mask=np.array(alpha)>127
    x=(np.arange(n)[None,:]+.5-n*.5)/(n*.5)
    y=(n*.5-np.arange(n)[:,None]-.5)/(n*.5)
    alpha = Image.fromarray((mask * 255).astype('uint8'))
    inner = np.array(alpha.filter(ImageFilter.MinFilter(7))) / 255
    rim = np.clip(mask.astype(float)-inner, 0, 1)
    shine = np.exp(-((x+.16)**2/.21 + (y-.67)**2/.12))
    softshade = np.clip(.35*x-.3*y, -.4, .5)
    variants=[]
    for white in [False, True]:
        value = (242 - 24*softshade + 13*shine) if white else (13 + 15*shine + 5*(1-y)/2)
        value = value*(1-rim) + rim*(98 if white else 174)
        rgba = np.zeros((n,n,4),np.uint8)
        rgba[:,:,:3] = np.clip(value[:,:,None],0,255)
        rgba[:,:,3] = mask*255
        image=Image.fromarray(rgba)
        d=ImageDraw.Draw(image)
        cx=n*(107/256);cy=n*(88/256);eye=n*(10/256)
        d.ellipse((cx-eye,cy-eye,cx+eye,cy+eye),fill=(17,17,21,255) if white else (249,249,248,255))
        image=image.resize((256,256),Image.Resampling.LANCZOS)
        variants.append(image);save(image,'Yang' if white else 'Yin')
    merged=Image.new('RGBA',(256,256))
    merged.alpha_composite(variants[0]);merged.alpha_composite(variants[1].rotate(180))
    save(merged,'Symbol')
    sheet=Image.new('RGBA',(768,256),(85,86,92,255))
    for i,img in enumerate(variants+[merged]):sheet.alpha_composite(img,(i*256,0))
    sheet.save(ART/'symbol_review.png')

def utility_textures():
    n=512
    y,x=np.mgrid[:n,:n].astype(float)
    x=(x-n*.5)/(n*.5);y=(y-n*.5)/(n*.5)
    r=np.sqrt(x*x+y*y);a=np.arctan2(y,x)
    # Compact hot flash with a halo, not a full opaque disc.
    core=np.exp(-(r/.31)**6); halo=.37*np.exp(-(r/.56)**2)
    alpha=np.clip(core+halo,0,1)*np.clip((1-r)/.14,0,1)
    rgba=np.full((n,n,4),255,np.uint8);rgba[:,:,3]=(alpha*255).astype('uint8')
    save(Image.fromarray(rgba),'Flash')
    # Broken ink rim with variable thickness and irregular chips.
    rad=.81+.012*np.sin(7*a)+.008*np.sin(19*a+1.2)+.004*np.sin(43*a)
    width=.013+.010*(.5+.5*np.sin(5*a+1))+.008*(.5+.5*np.sin(13*a))
    rng=np.random.default_rng(61)
    noise=gaussian_filter(rng.random((n,n)),1.0)
    rim=np.clip(1-abs(r-rad)/width,0,1)
    rim*=np.clip((np.sin(9*a+1)+.8)*2,0,1)*np.clip((noise-.3)*4,0,1)
    rgba[:,:,3]=(np.clip(rim,0,1)*255).astype('uint8')
    save(Image.fromarray(rgba),'Shockwave')
    shards=Image.new('RGBA',(8*64,64))
    rng=np.random.default_rng(120)
    for i in range(8):
        tile=Image.new('RGBA',(256,256));d=ImageDraw.Draw(tile)
        vertices=[]
        for k in range(7):
            ang=k*math.tau/7; rr=rng.uniform(.48,1)
            vertices.append((128+math.cos(ang)*rr*112,128+math.sin(ang)*rr*81))
        d.polygon(vertices,fill='white')
        d.polygon([(135,95),(167,72),(150,124)],fill=(0,0,0,0))
        shards.alpha_composite(tile.resize((64,64),Image.Resampling.LANCZOS),(i*64,0))
    save(shards,'Shards')

def burst(source):
    source=Image.open(source).convert('RGBA')
    source.save(ART/'burst_source.png')
    # Work in premultiplied color while resampling transparent artwork.
    src=np.array(source.resize((768,768),Image.Resampling.LANCZOS)).astype(float)/255
    src[:,:,:3]*=src[:,:,3:4]
    n=384
    y,x=np.mgrid[:n,:n].astype(float)
    x=(x+.5-n*.5)/(n*.5);y=(y+.5-n*.5)/(n*.5)
    r=np.hypot(x,y);ang=np.arctan2(y,x)
    rng=np.random.default_rng(210)
    noise=gaussian_filter(rng.random((n,n)),3)
    noise=(noise-noise.min())/(noise.max()-noise.min())
    fine=gaussian_filter(rng.random((n,n)),.8)
    atlas=Image.new('RGBA',(n*6,n*4)); review=Image.new('RGB',(384*4,384*2),(91,80,69))
    for frame in range(24):
        t=frame/23
        expansion=.22+.73*(1-math.exp(-t*11))
        twist=ang+.065*t+ .016*math.sin(t*9)*np.sin(ang*5+r*9)
        warp=1+.026*np.sin(ang*9+r*14-t*8)*t
        sx=(np.cos(twist)*r/expansion*warp+1)*383.5
        sy=(np.sin(twist)*r/expansion*warp+1)*383.5
        sampled=np.stack([map_coordinates(src[:,:,c],[sy,sx],order=1,mode='constant',cval=0) for c in range(4)],axis=2)
        # The hot center tears open first, then the broken edges erode into fragments.
        progress=max(0,(t-.15)/.85)
        hole=np.clip((r-(progress*.68-.06))/.10,0,1)
        erosion=np.clip((noise+.44*r-progress*1.25)*7,0,1)
        fade=(1-t)**.48 if frame<23 else 0
        opacity=hole*erosion*fade
        sampled[:,:,3]*=opacity
        color=np.divide(sampled[:,:,:3],np.maximum(sampled[:,:,3:4]/np.maximum(opacity[:,:,None],1e-7),1e-7))
        rgba=np.concatenate([np.clip(color,0,1),sampled[:,:,3:4]],axis=2)
        tile=Image.fromarray((np.clip(rgba,0,1)*255).astype('uint8'))
        atlas.alpha_composite(tile,((frame%6)*n,(frame//6)*n))
        if frame in [0,2,4,7,10,13,17,21]:
            i=[0,2,4,7,10,13,17,21].index(frame)
            review.paste(tile,((i%4)*384,(i//4)*384),tile)
    save(atlas,'Burst');review.save(ART/'burst_review.png')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,default=ART/'burst_source.png')
    args=parser.parse_args();symbols();utility_textures();burst(args.source)
    (ART/'manifest.json').write_text(json.dumps({'version':2,'symbolResolution':256,'burstFrameSize':384,'burstFrames':24,'burstGrid':[6,4],'alpha':'smooth RGBA','filter':'bilinear','orbitLine':False,'generator':'Tools/prepare_yinyang_v2.py','source':'built-in imagegen, reference-guided ink detonation'},indent=2))
    print('Baked YinYang V2 symbols, utilities and 24-frame burst atlas.')
