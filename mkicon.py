from PIL import Image, ImageDraw
S=1024; k=S/100
def glyph():
    m=Image.new('L',(S,S),0); d=ImageDraw.Draw(m)
    P=lambda pts:[(x*k,y*k) for x,y in pts]
    def minaret(cx):
        d.rounded_rectangle([ (cx-8)*k,44*k,(cx+8)*k,82*k],radius=2*k,fill=255)
        d.polygon(P([(cx-6,44),(cx+6,44),(cx+3,28),(cx-3,28)]),fill=255)
        d.ellipse([(cx-7.5)*k,13*k,(cx+7.5)*k,28*k],fill=255)      # crescent
        d.ellipse([(cx-3.5)*k,10*k,(cx+11.5)*k,25*k],fill=0)
    minaret(20); minaret(80)
    d.rectangle([26*k,56*k,74*k,82*k],fill=255)
    d.ellipse([29*k,30*k,71*k,82*k],fill=255)                       # dome
    d.polygon(P([(48,32),(52,32),(50,24)]),fill=255)
    d.rounded_rectangle([14*k,74*k,86*k,84*k],radius=3*k,fill=255)
    # door arch
    d.rectangle([42*k,68*k,58*k,84.5*k],fill=0); d.ellipse([42*k,59*k,58*k,77*k],fill=0)
    return m
g=glyph()
# tray: white glyph
tray=Image.new('RGBA',(S,S),(255,255,255,0)); tray.putalpha(g)
tray.resize((64,64),Image.LANCZOS).save('tray.png')
# exe icon: blue rounded square
bg=Image.new('RGBA',(S,S),(0,0,0,0)); mask=Image.new('L',(S,S),0)
ImageDraw.Draw(mask).rounded_rectangle([40,40,S-40,S-40],radius=230,fill=255)
grad=Image.new('RGBA',(S,S))
for y in range(S):
    t=y/S; c=(int(80-60*t),int(190-80*t),int(255-20*t),255)
    ImageDraw.Draw(grad).line([(0,y),(S,y)],fill=c)
bg.paste(grad,(0,0),mask)
w=Image.new('RGBA',(S,S),(255,255,255,0)); w.putalpha(g)
sm=w.resize((int(S*.72),)*2,Image.LANCZOS); bg.alpha_composite(sm,((S-sm.width)//2,(S-sm.height)//2+10))
bg.resize((256,256),Image.LANCZOS).save('app.png')
bg.resize((256,256),Image.LANCZOS).save('app.ico',sizes=[(16,16),(20,20),(24,24),(32,32),(40,40),(48,48),(64,64),(128,128),(256,256)])
# preview sheet
sheet=Image.new('RGBA',(520,140),(32,32,32,255))
sheet.alpha_composite(bg.resize((128,128),Image.LANCZOS),(6,6))
t=tray.resize((64,64),Image.LANCZOS); sheet.alpha_composite(t,(150,40))
t16=tray.resize((16,16),Image.LANCZOS).resize((64,64),Image.NEAREST); sheet.alpha_composite(t16,(240,40))
b16=bg.resize((16,16),Image.LANCZOS).resize((64,64),Image.NEAREST); sheet.alpha_composite(b16,(330,40))
sheet.save('/downloads/icon-preview.png')
