"""Regenerate the multi-resolution executable icon. Requires Pillow (development only)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math
S=768
im=Image.new('RGBA',(S,S))
pixels=im.load()
for y in range(S):
    for x in range(S):
        nx,ny=x/S,y/S
        if (nx-.5)**2+(ny-.5)**2 < .465**2:
            t=min(1,math.hypot(nx-.35,ny-.24)/.8)
            stops=[(230,255,255),(38,223,237),(0,142,184)]
            a,b=(stops[0],stops[1]) if t<.43 else (stops[1],stops[2])
            u=t/.43 if t<.43 else (t-.43)/.57
            pixels[x,y]=tuple(round(a[i]+(b[i]-a[i])*u) for i in range(3))+(255,)
d=ImageDraw.Draw(im)
d.ellipse((27,27,741,741),outline=(204,255,255,255),width=9)
sheen=Image.new('RGBA',im.size); ImageDraw.Draw(sheen).ellipse((108,87,606,309),fill=(255,255,255,65)); im=Image.alpha_composite(im,sheen)
d=ImageDraw.Draw(im)
d.polygon([(249,573),(249,225),(525,225),(525,297),(333,297),(333,369),(486,369),(486,441),(333,441),(333,573)],fill='white')
d.ellipse((460,95,633,226),fill=(87,244,150),outline=(190,255,218),width=5)
d.line([(465,220),(600,125)],fill=(230,255,205),width=5)
im=im.resize((256,256),Image.Resampling.LANCZOS)
im.save(Path(__file__).resolve().parents[1]/'FreshOS/Assets/FreshOS.ico',sizes=[(s,s) for s in [16,24,32,48,64,128,256]])
