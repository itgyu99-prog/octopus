"""Approval-only fog study. Runtime assets remain unchanged until final approval."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Art/YinYangFogReview';OUT.mkdir(parents=True,exist_ok=True)
W,H=768,432
rng=np.random.default_rng(281)
layers=[]
for n,weight in [(5,1),(11,.6),(23,.3),(47,.15),(95,.07)]:
 a=rng.random((n,n*2),dtype=np.float32)
 layers.append((np.asarray(Image.fromarray(a).resize((W*2,H*2),Image.Resampling.BICUBIC)),weight))
base=Image.open(ROOT/'Art/Review/bottom-hud-full.png').convert('RGB').resize((W,H))
original=np.asarray(base)
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',14)
# Re-composite the photographed HUD above the world-only fog. No rectangular
# exclusion zones: fog must remain continuous on the ground around every button.
ui=Image.new('L',(W,H),0)
d=ImageDraw.Draw(ui)
for cy in [128,164,199,234,269,304]:
 d.ellipse((12,cy-15,42,cy+15),fill=255)
d.ellipse((687,329,756,398),fill=255)
pixels=original.astype(np.int16)
hud=(pixels[:,:,0]-pixels[:,:,2]<64)|(pixels[:,:,0]<95)
roi=np.zeros((H,W),dtype=bool);roi[349:426,185:583]=True
# Flood-fill the small HUD crop from its outside boundary.
from collections import deque
silhouette=hud[347:428,183:585]
outside=np.zeros(silhouette.shape,dtype=bool)
queue=deque([(0,0)])
while queue:
 y,x=queue.popleft()
 if y<0 or x<0 or y>=silhouette.shape[0] or x>=silhouette.shape[1]:continue
 if outside[y,x] or silhouette[y,x]:continue
 outside[y,x]=True
 queue.extend(((y-1,x),(y+1,x),(y,x-1),(y,x+1)))
inside=np.zeros((H,W),dtype=bool)
inside[347:428,183:585]=~outside
ui=np.maximum(np.asarray(ui),np.uint8(inside)*255)
# Text/icons keep their original pixels and contrast.
text=np.zeros((H,W),dtype=bool)
white=(pixels[:,:,0]>210)&(pixels[:,:,1]>200)&(pixels[:,:,2]>180)
text[402:419,14:124]=white[402:419,14:124]
gold=(pixels[:,:,0]>210)&(pixels[:,:,1]>205)&(pixels[:,:,2]>140)
text[8:26,579:754]=gold[8:26,579:754]
ui=np.maximum(ui,np.uint8(text)*255)
pearl=Image.new('L',(W,H),0);ImageDraw.Draw(pearl).ellipse((581,11,594,24),fill=255)
ui=np.maximum(ui,np.asarray(pearl))
protect=ui>0
assert .02 < protect.mean() < .2, 'UI mask must not erase world fog'
Image.fromarray(ui).save(OUT/'ui_protection_mask.png')
frames=[]
def smooth(x):
 x=np.clip(x,0,1);return x*x*(3-2*x)
for f in range(120):
 t=f/15
 cloud=np.zeros((H,W),dtype=np.float32)
 for i,(a,w) in enumerate(layers):
  x=int(70+i*39+t*(3+i*.6));y=int(40+i*24+t*(1.2-i*.25))
  cloud+=a[y:y+H,x:x+W]*w
 cloud=cloud/2.12
 billow=smooth((cloud-.22)/.53)
 detail=np.clip((cloud-.35)*2.0,0,1)
 # Higher minimum coverage fills the formerly bare patches; lower highlights
 # make the bank slightly more transparent without erasing its fine structure.
 alpha=(.48+.25*billow)*smooth(t/2.8)*(1-smooth((t-6.4)/1.6))
 lum=np.clip(125+110*billow+8*detail,0,255)
 fog=np.repeat(lum[:,:,None],3,axis=2)
 arr=np.uint8(original.astype(np.float32)*(1-alpha[:,:,None])+fog*alpha[:,:,None])
 arr[protect]=original[protect]
 assert np.array_equal(arr[protect],original[protect]),'UI pixels changed'
 # Caption is outside the image, so it cannot obscure the stage interface.
 im=Image.new('RGB',(W,H+36),(20,23,28));im.paste(Image.fromarray(arr),(0,36))
 draw=ImageDraw.Draw(im)
 draw.text((12,10),'음양 안개 수정안 · 빈틈 보강 / 약간 더 투명 / UI는 안개 위',font=font,fill=(240,240,240))
 frames.append(im)
frames[60].save(OUT/'fog_keyframe_v2.png')
frames[0].save(OUT/'fog_preview_v2_review.gif',save_all=True,append_images=frames[1:],duration=67,loop=0,optimize=False)
print('Saved V2 GIF; all protected HUD pixels unchanged in all 120 source frames.')
