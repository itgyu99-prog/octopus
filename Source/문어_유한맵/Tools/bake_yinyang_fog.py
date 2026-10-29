"""Bake the approved V2 cloud colours and coverage into a shared runtime atlas."""
from pathlib import Path
import uuid
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Resources/Art/YinYang/Fog.bytes'
W,H=768,432
TW,TH=512,288
rng=np.random.default_rng(281)
layers=[]
for n,weight in [(5,1),(11,.6),(23,.3),(47,.15),(95,.07)]:
    a=rng.random((n,n*2),dtype=np.float32)
    noise=np.asarray(Image.fromarray(a).resize((W*2,H*2),Image.Resampling.BICUBIC))
    # Extend the approved source rather than stretching its camera-sized patch.
    # The central normal-camera viewport retains the approved cloud scale.
    layers.append((np.pad(noise,((H,H),(W,W)),mode='reflect'),weight))
def smooth(x):
    x=np.clip(x,0,1)
    return x*x*(3-2*x)
atlas=Image.new('RGBA',(TW*8,TH*8))
for frame in range(64):
    t=frame/8
    cloud=np.zeros((H*2,W*2),dtype=np.float32)
    for i,(a,w) in enumerate(layers):
        x=int(70+i*39+t*(3+i*.6));y=int(40+i*24+t*(1.2-i*.25))
        cloud+=a[y+H//2:y+H//2+H*2,x+W//2:x+W//2+W*2]*w
    cloud/=2.12
    billow=smooth((cloud-.22)/.53)
    detail=np.clip((cloud-.35)*2,0,1)
    # Envelope is applied in Unity from replicated skill age, not baked here.
    alpha=.48+.25*billow
    lum=np.clip(125+110*billow+8*detail,0,255)
    rgba=np.empty((H*2,W*2,4),dtype=np.uint8)
    rgba[:,:,:3]=lum[:,:,None]
    rgba[:,:,3]=np.uint8(alpha*255)
    tile=Image.fromarray(rgba).resize((TW,TH),Image.Resampling.BILINEAR)
    assert np.asarray(tile)[:,:,3].min()>=121,'Fog contains a bare hole'
    atlas.paste(tile,((frame%8)*TW,(frame//8)*TH))
atlas.save(OUT,format='PNG',optimize=True)
meta=Path(str(OUT)+'.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
print('Baked approved fog:',atlas.size,OUT.stat().st_size,'bytes; 64 frames; no alpha holes')
