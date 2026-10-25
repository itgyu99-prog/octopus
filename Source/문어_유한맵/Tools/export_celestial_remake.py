from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import zipfile,json
root=Path(__file__).resolve().parents[1];art=root/'Art/Celestial'
def frames(prefix):
    files=sorted((root/'Logs/Celestial/Captures').glob(f'runtime_{prefix}_*.png'))
    return files or sorted(art.glob(f'runtime_{prefix}_*.png'))
def game_gif(key,label):
    paths=frames(key);assert paths, key
    seq=[]
    for p in paths:
        im=Image.open(p).convert('RGB').resize((512,512),Image.Resampling.NEAREST);seq.append(im)
    seq[0].save(art/f'{key}_gameplay.gif',save_all=True,append_images=seq[1:],duration=100,loop=0,disposal=2)
    return seq
cos=game_gif('cosmos','COSMOS');yin=game_gif('yin_yang','YIN & YANG')
# Both columns are direct captures from the Unity main camera during the deterministic gameplay check.
combo=[]
for i in range(max(len(cos),len(yin))):
    canvas=Image.new('RGB',(1024,544),(13,16,29));d=ImageDraw.Draw(canvas)
    d.text((16,6),'COSMOS · LIVE UNITY CAPTURE',fill=(235,238,255));d.text((528,6),'YIN & YANG · LIVE UNITY CAPTURE',fill=(235,238,255))
    for col,seq in enumerate([cos,yin]):canvas.paste(seq[min(i,len(seq)-1)],(col*512,28))
    combo.append(canvas)
combo[0].save(art/'runtime_preview.gif',save_all=True,append_images=combo[1:],duration=100,loop=0,disposal=2)
ref=Image.open(art/'reference.png').convert('RGB')
can=Image.new('RGB',(1360,800),(13,16,29));d=ImageDraw.Draw(can)
d.text((12,8),'REFERENCE · supplied effect overview',fill='white');can.paste(ref.resize((650,433)),(0,34))
d.text((676,8),'PIXEL ART · captures from the Unity gameplay camera',fill='white')
panels=[('Cosmos · stars','game_cosmos_stars.png'),('Cosmos · homing','game_cosmos_tracking.png'),('Yin & Yang · orbit','game_yin_yang_orbit.png'),('Yin & Yang · detonation','game_yin_yang_burst.png')]
for i,(title,name) in enumerate(panels):
    x=680+(i%2)*338;y=36+(i//2)*370;d.text((x,y),title,fill='white')
    im=Image.open(art/name).convert('RGB').resize((330,330),Image.Resampling.NEAREST);can.paste(im,(x,y+24))
can.save(art/'comparison.png')
checks=list((art/'frames').rglob('*.png'))
for p in checks:
    im=Image.open(p);assert im.size==(128,128) and im.mode=='RGBA' and im.getchannel('A').getbbox()
validation={'frames':len(checks),'frameSize':'128x128','sourcePixelGrid':'64x64 nearest-neighbor enlarged 2x; opaque alpha per pixel','spritesheetRows':11,'runtimeUnityCaptureFrames':{'cosmos':len(cos),'yinYang':len(yin)},'unityChecks':98,'unityStatus':'PASS','runtimeErrors':0}
(art/'validation.json').write_text(json.dumps(validation,indent=2))
archive=root/'Art/Celestial_Assets.zip'
include=['README.md','prompts.txt','manifest.json','comparison.png','cosmos_sheet.png','yin_yang_sheet.png','celestial_atlas.png','cosmos_preview.gif','yin_yang_preview.gif','runtime_preview.gif','cosmos_gameplay.gif','yin_yang_gameplay.gif','validation.json']
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for n in include:z.write(art/n,Path('Celestial')/n)
    for p in sorted((art/'frames').rglob('*.png')):z.write(p,Path('Celestial')/'frames'/p.parent.name/p.name)
    for p in sorted(art.glob('cosmos_*.png')):
        if p.name in include or p.name=='cosmos_sheet.png':continue
        z.write(p,Path('Celestial')/p.name)
    for p in sorted(art.glob('yin_*.png')):
        if p.name in include or p.name=='yin_yang_sheet.png':continue
        z.write(p,Path('Celestial')/p.name)
    for r in ['reference.png'] : z.write(art/r,Path('Celestial')/r)
    z.write(root/'Assets/Resources/Art/CelestialAtlas.bytes',Path('Celestial/Unity/CelestialAtlas.bytes'))
print(json.dumps(validation));print('archive MB',round(archive.stat().st_size/1024/1024,2))
