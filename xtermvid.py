#asdads
#!/usr/bin/env python3
"""xTerminal — Cinematic MP4 Presentation  1280×720 @ 24fps"""
import subprocess, math, sys, shutil, wave, tempfile, os
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

W, H, FPS = 1920, 1080, 60

OUT_DIR = Path.home() / "Videos" / "xTerminal"
OUT_DIR.mkdir(parents=True, exist_ok=True)
OUT = str(OUT_DIR / "xTerminal-presentation.mp4")

MF = r"C:\Windows\Fonts\consola.ttf"
BF = r"C:\Windows\Fonts\consolab.ttf"

# Palette
BG=(5,8,10); G=(0,255,136); G2=(0,180,90); C=(0,229,255); A=(255,179,0)
R=(255,77,77); M=(210,100,255); DIM=(55,100,70); TX=(180,240,200)
WH=(232,255,240); SRF=(13,22,16); BD=(22,48,36); BD2=(29,60,44)

_fc={}
def fnt(sz,b=False):
    k=(sz,b)
    if k not in _fc: _fc[k]=ImageFont.truetype(BF if b else MF,sz)
    return _fc[k]

def tw(s,f): bb=f.getbbox(s); return bb[2]-bb[0]
def ease(t): return 1-(1-max(0,min(1,t)))**2
def fade(t,s,e): return ease((t-s)/max(e-s,0.001)) if t>s else 0
def slide(t,s,e): return ease(max(0,min(1,(t-s)/max(e-s,0.001))))

# Pre-build grid as numpy array (reused every frame)
_grid=np.zeros((H,W,4),dtype=np.uint8)
for x in range(0,W,48): _grid[:,x]=[0,60,25,255]
for y in range(0,H,48): _grid[y,:]=[0,60,25,255]
GRID_IMG=Image.fromarray(_grid,'RGBA')

def new_frame():
    img=Image.new('RGB',(W,H),BG)
    return img

def draw_grid(img,alpha=0.4):
    g=GRID_IMG.copy()
    arr=np.array(g)
    arr[:,:,3]=(arr[:,:,3]*alpha).astype(np.uint8)
    ov=Image.fromarray(arr,'RGBA')
    img2=img.convert('RGBA')
    img2=Image.alpha_composite(img2,ov)
    return img2.convert('RGB')

def corners(d,pad=22):
    c=(*BD2,); sz=50
    def L(x0,y0,x1,y1): d.line([(x0,y0),(x1,y1)],fill=c,width=1)
    L(pad,pad,pad+sz,pad); L(pad,pad,pad,pad+sz)
    L(W-pad,pad,W-pad-sz,pad); L(W-pad,pad,W-pad,pad+sz)
    L(pad,H-36,pad+sz,H-36); L(pad,H-36,pad,H-36-sz)
    L(W-pad,H-36,W-pad-sz,H-36); L(W-pad,H-36,W-pad,H-36-sz)

def term_win(img,x,y,w,h,title="xTerminal"):
    """Draw terminal window, return (img, body_x, body_y, body_w, body_h)"""
    bh=30
    ov=Image.new('RGBA',img.size,(0,0,0,0))
    od=ImageDraw.Draw(ov)
    # Shadow
    od.rectangle([(x+4,y+4),(x+w+4,y+h+4)],fill=(0,0,0,100))
    # Body
    od.rectangle([(x,y),(x+w,y+h)],fill=(*SRF,250))
    # Title bar
    od.rectangle([(x,y),(x+w,y+bh)],fill=(8,16,11,255))
    # Border
    od.rectangle([(x,y),(x+w,y+h)],outline=(*BD,200),width=1)
    # Dots
    for i,dc in enumerate([(255,95,86),(255,189,46),(39,201,63)]):
        cx=x+12+i*16; cy=y+bh//2
        od.ellipse([(cx-5,cy-5),(cx+5,cy+5)],fill=dc)
    # Title
    tf=fnt(11); ttw=tw(title,tf)
    od.text((x+w//2-ttw//2,y+8),title,font=tf,fill=DIM)
    img2=img.convert('RGBA')
    img2=Image.alpha_composite(img2,ov)
    return img2.convert('RGB'), x+1, y+bh+1, w-2, h-bh-2

def tline(d,x,y,parts,f):
    cx=x
    for txt,col in parts:
        d.text((cx,y),txt,font=f,fill=col)
        cx+=tw(txt,f)

def hud(d,si,total,t,dur):
    names=["INTRO","SHELL","NETWORKING","TERMXT","WTOP","AI · CGPT","C# RUNNER","SECURITY","POWER TOOLS","FIN"]
    # Bar bg
    d.rectangle([(0,H-24),(W,H)],fill=(5,8,10))
    d.line([(0,H-24),(W,H-24)],fill=BD,width=1)
    # Progress fill
    prog=int(W*((si+min(t/max(dur,0.001),1))/total))
    d.rectangle([(0,H-3),(prog,H)],fill=G)
    d.rectangle([(0,H-3),(W,H)],outline=BD,width=0)
    # Label
    lf=fnt(11)
    nm=names[min(si,len(names)-1)]
    d.text((16,H-18),f"// {nm}",font=lf,fill=C)
    cnt=f"{si+1:02d} / {total:02d}"
    d.text((W-tw(cnt,lf)-16,H-18),cnt,font=lf,fill=DIM)
    # Dots
    sx=W//2-total*9
    for i in range(total):
        cx=sx+i*18+9; cy=H-13
        col=G if i==si else BD2
        r=4 if i==si else 2
        d.ellipse([(cx-r,cy-r),(cx+r,cy+r)],fill=col)

def feat_header(d,img,t,tag_str,title_lines,desc_lines=None):
    tf=fnt(11); bf=fnt(42,True); df=fnt(13)
    ta=fade(t,0,0.7)
    if ta>0:
        d.text((60,120),tag_str,font=tf,fill=tuple(int(c*ta) for c in C))
    y=155
    for i,line in enumerate(title_lines):
        la=fade(t,0.1+i*0.15,0.7+i*0.15)
        if la>0:
            # Fake glow: draw dark version offset
            col=tuple(int(c*la) for c in G)
            dim_col=tuple(int(c*la*0.15) for c in G)
            for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)]:
                d.text((60+dx,y+dy),line,font=bf,fill=dim_col)
            d.text((60,y),line,font=bf,fill=col)
        y+=52
    if desc_lines:
        dy2=y+10
        for i,dline in enumerate(desc_lines):
            la=fade(t,0.4+i*0.08,0.85+i*0.08)
            if la>0:
                col=tuple(int(c*la) for c in DIM)
                d.text((60,dy2),dline,font=df,fill=col)
                dy2+=20
    return y

def divider(d,x=W//2-20):
    for y in range(100,H-30):
        a=int(45*math.sin(math.pi*(y-100)/(H-130)))
        d.point((x,y),fill=(*BD2,a))

# ─── AUDIO ────────────────────────────────────────────────────────────────
# 22050 Hz is sufficient — highest frequency component is 1320 Hz (Nyquist: 11025 Hz)
SAMPLE_RATE = 22050

def generate_audio(total_duration):
    """Ambient cinematic background track generated with pure numpy."""
    sr  = SAMPLE_RATE
    n   = int(total_duration * sr)
    # float32 throughout: ~2× faster SIMD trig + half the memory vs float64
    t   = np.arange(n, dtype=np.float32) * np.float32(total_duration / n)
    pi2 = np.float32(2 * np.pi)

    # Deep bass drone
    bass  = np.float32(0.18) * np.sin(pi2 * np.float32(55.0)  * t)   # A1 sub-bass
    bass += np.float32(0.10) * np.sin(pi2 * np.float32(82.4)  * t)   # E2 fifth
    bass += np.float32(0.06) * np.sin(pi2 * np.float32(110.0) * t)   # A2 octave

    # Slowly evolving pad chords (AM-modulated oscillators)
    lfo1 = np.float32(0.5) + np.float32(0.5) * np.sin(pi2 * np.float32(0.05) * t)
    lfo2 = np.float32(0.5) + np.float32(0.5) * np.sin(pi2 * np.float32(0.07) * t + np.float32(1.0))
    lfo3 = np.float32(0.5) + np.float32(0.5) * np.sin(pi2 * np.float32(0.09) * t + np.float32(2.5))
    pad  = np.float32(0.07) * lfo1 * np.sin(pi2 * np.float32(220.0) * t)  # A3
    pad += np.float32(0.05) * lfo2 * np.sin(pi2 * np.float32(261.6) * t)  # C4
    pad += np.float32(0.05) * lfo3 * np.sin(pi2 * np.float32(329.6) * t)  # E4
    pad += np.float32(0.04) * lfo1 * np.sin(pi2 * np.float32(440.0) * t)  # A4

    # High shimmer
    shimmer  = np.float32(0.025) * lfo3 * np.sin(pi2 * np.float32(880.0)  * t)
    shimmer += np.float32(0.015) * lfo1 * np.sin(pi2 * np.float32(1320.0) * t)

    # Subtle rhythmic pulse at 80 BPM
    beat_env = np.exp(np.float32(-6.0) * np.mod(t * np.float32(80.0 / 60.0), np.float32(1.0)))
    pulse    = np.float32(0.06) * beat_env * np.sin(pi2 * np.float32(110.0) * t)

    mono = np.tanh((bass + pad + shimmer + pulse) * np.float32(2.5)) * np.float32(0.4)

    # Fade in / fade out
    fi = min(int(2 * sr), n // 4)
    fo = min(int(2 * sr), n // 4)
    mono[:fi]  *= np.linspace(np.float32(0.0), np.float32(1.0), fi, dtype=np.float32) ** 2
    mono[-fo:] *= np.linspace(np.float32(1.0), np.float32(0.0), fo, dtype=np.float32) ** 2

    # Stereo width via small right-channel delay
    delay = int(0.008 * sr)
    right = np.concatenate([np.zeros(delay, dtype=np.float32), mono[:-delay]])
    return np.stack([mono, right], axis=1)

def save_wav(stereo, path):
    """Write float32 stereo array (N,2) in [-1,1] to a 16-bit stereo WAV file."""
    data_i16 = (np.clip(stereo, -1.0, 1.0) * 32767).astype(np.int16)
    with wave.open(path, 'w') as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(data_i16.tobytes())

# ─── SCENE 0: INTRO ───────────────────────────────────────────────────────
def s_intro(t):
    img=new_frame()
    g_alpha=0.35+0.15*math.sin(t*1.3)
    img=draw_grid(img,g_alpha)
    d=ImageDraw.Draw(img)
    corners(d)

    # Logo
    lf=fnt(80,True); logo="xTerminal"
    lw=tw(logo,lf)
    la=fade(t,0,1.0)
    if la>0:
        col=tuple(int(c*la) for c in G)
        dc =tuple(int(c*la*0.18) for c in G)
        for dx,dy in [(-2,0),(2,0),(0,-2),(0,2),(-1,-1),(1,1)]:
            d.text((W//2-lw//2+dx,H//2-220+dy),logo,font=lf,fill=dc)
        d.text((W//2-lw//2,H//2-220),logo,font=lf,fill=col)

    # Tagline
    tag="A Linux-like shell for Windows  ·  Written in C#  ·  .NET 10"
    tf=fnt(14); tw2=tw(tag,tf)
    ta=fade(t,0.9,1.8)
    if ta>0:
        d.text((W//2-tw2//2,H//2-108),tag,font=tf,fill=tuple(int(c*ta) for c in DIM))

    # GitHub
    gh="github.com/0x78654C/xTerminal"
    gf=fnt(15); gw=tw(gh,gf)
    ga=fade(t,1.4,2.2)
    if ga>0:
        d.text((W//2-gw//2,H//2-78),gh,font=gf,fill=tuple(int(c*ga) for c in C))

    # Stats
    stats=[("112","Stars"),("v3.0","Latest"),(".xt","Script")]
    sw=len(stats)*190; sx=W//2-sw//2
    for i,(v,l) in enumerate(stats):
        sa=fade(t,1.9+i*0.25,2.6+i*0.25)
        if sa>0:
            vf=fnt(36,True); lf2=fnt(11)
            vw2=tw(v,vf); lw2=tw(l,lf2)
            cx=sx+i*190+95
            d.text((cx-vw2//2,H//2-22),v,font=vf,fill=tuple(int(c*sa) for c in G))
            d.text((cx-lw2//2,H//2+22),l,font=lf2,fill=tuple(int(c*sa) for c in DIM))

    # Pills
    cats=[("System",G),("Networking",C),("Scripting",G),("AI",A),("Security",M),("C# Runner",C)]
    px=W//2-len(cats)*90; py=H//2+60
    for i,(nm,col) in enumerate(cats):
        pa=fade(t,3.0+i*0.15,3.6+i*0.15)
        if pa>0:
            pf2=fnt(12); pw=tw(nm,pf2)+18
            ov=Image.new('RGBA',img.size,(0,0,0,0))
            od=ImageDraw.Draw(ov)
            od.rounded_rectangle([(px,py),(px+pw,py+26)],radius=3,
                outline=(*col,int(pa*180)),fill=(*col,int(pa*18)))
            od.text((px+9,py+6),nm,font=pf2,fill=(*col,int(pa*255)))
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov)
            img=img.convert('RGB'); d=ImageDraw.Draw(img)
            px+=pw+8
    return img

# ─── SCENE 1: SHELL ───────────────────────────────────────────────────────
def s_shell(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// Linux-like Shell for Windows",["The Shell","You Know"])
    bf=fnt(42,True)
    items=[
        ("TAB×2  ","→  Smart autocomplete for files & directories"),
        ("alias  ","→  Custom shortcuts with inline parameters"),
        ("ch     ","→  Searchable history, up to 2000 entries"),
        ("watch  ","→  Auto-rerun any command on a timer"),
        ("fsmon  ","→  Real-time filesystem change monitor"),
        ("plist  ","→  Process tree view"),
    ]
    iy=270
    for i,(kw,desc) in enumerate(items):
        la=fade(t,0.3+i*0.1,0.75+i*0.1)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in G))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"xTerminal — Shell")
        d=ImageDraw.Draw(img)
        mf=fnt(13); lh=23
        cy=by+10
        def L(parts,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh:
                tline(d,bx+10,cy,parts,mf); cy+=lh
        def B(st):
            nonlocal cy
            if t>=st: cy+=lh//2
        L([("> ",DIM),("ls",G)],0.6)
        L([("Commands/  Core/  Shell/  README.md",TX)],1.1)
        B(1.1)
        L([("> ",DIM),("pcinfo",G)],1.7)
        L([("CPU  Intel Core i7-12700K  ·  MEM 46.9% of 32 GB",C)],2.1)
        L([("OS   Windows 11 Pro 24H2   ·  HOST DESKTOP-MRX",TX)],2.35)
        B(2.35)
        L([("> ",DIM),("sinfo",G)],2.9)
        L([("Model         Samsung SSD 980 PRO 1TB",TX)],3.35)
        L([("Size          1000 GB  ·  MediaType  SSD",TX)],3.55)
        B(3.55)
        L([("> ",DIM),("alias ",G),("-add lz*ls -s",WH)],4.1)
        L([("+  Alias 'lz' created  ->  runs 'ls -s'",G)],4.6)
        B(4.6)
        L([("> ",DIM),("lz",G)],5.2)
        L([("Commands/ 1.2MB   Core/ 890KB   Shell/ 2.1MB   README.md 14KB",TX)],5.7)
        B(5.7)
        L([("> ",DIM),("watch ",G),("-n 5 ",WH),("\"plist\"",A)],6.3)
        if t>=6.8 and cy+lh<by+bh:
            d.text((bx+10,cy),"[Refreshing every 5 seconds…]",font=fnt(12),fill=DIM)
    return img

# ─── SCENE 2: NETWORKING ──────────────────────────────────────────────────
def s_net(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// Networking Suite",["Network","Arsenal"])
    cmds=[
        ("latmon ","multi-host ping monitor + sparklines"),
        ("cport  ","port scanner  (range: 1-1000)"),
        ("trace  ","traceroute with hops & timeout"),
        ("dspoof ","ARP MITM attack detection"),
        ("ispeed ","internet speed test via Google"),
        ("wol    ","Wake-on-LAN packet sender"),
        ("extip  ","show your external IP address"),
        ("ifconfig","NIC configuration viewer"),
    ]
    iy=275
    for i,(kw,desc) in enumerate(cmds):
        la=fade(t,0.25+i*0.09,0.65+i*0.09)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in G))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"xTerminal — Networking")
        d=ImageDraw.Draw(img)
        mf=fnt(12); lh=22
        cy=by+10
        def L(parts,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh: tline(d,bx+8,cy,parts,mf); cy+=lh
        def B(st):
            nonlocal cy
            if t>=st: cy+=lh//2
        L([("> ",DIM),("latmon ",G),("google.com cloudflare.com 8.8.8.8",WH)],0.7)

        L([("latmon  interval:1000ms  ·  Q/Esc quit  ·  ▁▂▃▄▅▆▇█",DIM)],0.9)
        B(0.9)
        # header
        if t>=1.2:
            hf=fnt(11)
            d.text((bx+8,cy),"HOST".ljust(18)+"CUR".ljust(8)+"AVG".ljust(8)+"MIN".ljust(8)+"MAX".ljust(8)+"LOSS".ljust(7)+"HISTORY",font=hf,fill=DIM)
            cy+=20
            d.line([(bx+8,cy),(bx+bw-8,cy)],fill=BD,width=1); cy+=4
        rows=[
            (1.5,"google.com    ","17ms","17ms","17ms","18ms","0% ","▂▃▂▃▃▂▃▂",G),
            (2.1,"cloudflare.com","9ms ","10ms","8ms ","12ms","0% ","▁▂▁▁▂▁▂▁",G),
            (2.7,"8.8.8.8       ","62ms","58ms","52ms","67ms","0% ","▄▅▄▅▆▅▄▅",A),
        ]
        for (st,host,cur,avg,mn,mx,loss,spark,col) in rows:
            if t>=st and cy+lh<by+bh:
                rf=fnt(13)
                p=[(host,col),(cur,col),(avg,col),(mn,col),(mx,col),(loss,G),(spark,col)]
                tline(d,bx+8,cy,p,rf); cy+=lh
        B(3.1)
        L([("> ",DIM),("cport ",G),("github.com -p 22-443",WH)],3.3)
        if t>=3.9 and cy+lh<by+bh:
            pf2=fnt(13)
            for j,(port,open_) in enumerate([("22",True),("80",True),("443",True)]):
                px=bx+8+j*170
                col2=G if open_ else R
                d.ellipse([(px,cy+5),(px+8,cy+13)],fill=col2)
                d.text((px+12,cy),f"Port {port} {'OPEN' if open_ else 'CLOSED'}",font=pf2,fill=col2)
            cy+=lh
        B(4.2)
        if t>=4.4 and cy+lh<by+bh:
            d.text((bx+8,cy),"+  3 open / 0 closed on github.com",font=fnt(13),fill=G); cy+=lh
        B(4.7)
        L([("> ",DIM),("extip",G)],5.0)
        L([("External IP: 185.220.101.42",C)],5.5)
        B(5.5)
        L([("> ",DIM),("dspoof",G)],6.1)
        L([(">>  No ARP spoofing detected on network",G)],6.7)
        B(6.7)
        L([("> ",DIM),("trace ",G),("8.8.8.8 -hops 10",WH)],7.3)
        if t>=7.7 and cy+lh<by+bh:
            for hop,ip,ms in [("1","192.168.1.1","2ms"),("2","10.0.0.1","5ms"),("3","8.8.8.8","17ms")]:
                if cy+lh<by+bh:
                    d.text((bx+8,cy),f"  {hop:>2}  {ip:<16} {ms}",font=fnt(12),fill=TX); cy+=lh
    return img

# ─── SCENE 3: TERMXT ──────────────────────────────────────────────────────
def s_xt(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// TermXT Scripting Language",["Script","Everything"])
    items=[
        ("set / eval   ","variables & math expressions"),
        ("capture var  ","= store command output"),
        ("each … in    ","lists, ranges, output lines"),
        ("func / call  ","reusable named functions"),
        ("try / catch  ","error handling blocks"),
        ("{DATE}{TIME} ","built-in system variables"),
        ("xt -check    ","validate without running"),
    ]
    iy=275
    for i,(kw,desc) in enumerate(items):
        la=fade(t,0.25+i*0.09,0.65+i*0.09)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in C))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"TermXT Editor — healthcheck.xt")
        d=ImageDraw.Draw(img)
        DG=(45,80,55)
        code=[
            (0.7,  "1 ",[("# TermXT health-check script",DIM)]),
            (0.8,  "2 ",[]),
            (0.9,  "3 ",[("set ",(200,90,50)),("target",TX),(" = ",DG),("{1}",A)]),
            (1.3,  "4 ",[("capture ",(200,90,50)),("ip",TX),(" = ",DG),("extip",C)]),
            (1.7,  "5 ",[("print ",(200,90,50)),("\"External IP: {ip} · {DATE}\"",A)]),
            (2.1,  "6 ",[]),
            (2.2,  "7 ",[("set ",(200,90,50)),("hosts",TX),(" = ",DG),("google.com,cloudflare.com,8.8.8.8",A)]),
            (2.7,  "8 ",[("each ",(200,90,50)),("host",TX),(" in ",(200,90,50)),("{hosts}",C)]),
            (3.1,  "9 ",[("    print ",(200,90,50)),("\"Checking {host}…\"",A)]),
            (3.5, "10 ",[("    run ",(200,90,50)),("cport",C),(" {host} -p 443 --noping",TX)]),
            (3.9, "11 ",[("end",(200,90,50))]),
            (4.2, "12 ",[]),
            (4.3, "13 ",[("try",(200,90,50))]),
            (4.6, "14 ",[("    run ",(200,90,50)),("trace",C),(" {target} -hops 20",TX)]),
            (5.0, "15 ",[("catch",(200,90,50))]),
            (5.3, "16 ",[("    print ",(200,90,50)),("\"trace failed: {error_message}\"",A)]),
            (5.7, "17 ",[("end",(200,90,50))]),
            (6.0, "18 ",[]),
            (6.1, "19 ",[("write ",(200,90,50)),("\"report_{DATE}.txt\"",A),(" {ip}",TX)]),
        ]
        lnf=fnt(11); cf=fnt(13); lh=24; cy=by+8
        for (st,lno,parts) in code:
            if t>=st and cy+lh<by+bh:
                d.text((bx+6,cy),lno.rjust(3),font=lnf,fill=BD)
                cx2=bx+6+36
                for (txt,col) in parts:
                    d.text((cx2,cy+1),txt,font=cf,fill=col); cx2+=tw(txt,cf)
                cy+=lh
        # Run output
        if t>=7.0 and cy+lh<by+bh:
            d.line([(bx+6,cy),(bx+bw-6,cy)],fill=BD); cy+=6
            d.text((bx+6,cy),"> xt healthcheck.xt -p github.com",font=fnt(12),fill=DIM); cy+=22
        if t>=7.5 and cy+lh<by+bh:
            d.text((bx+6,cy),"External IP: 185.220.101.42 · 2025-05-14",font=fnt(13),fill=G); cy+=22
        if t>=7.9 and cy+lh<by+bh:
            d.text((bx+6,cy),"Checking google.com...    +  443 OPEN",font=fnt(13),fill=TX); cy+=22
        if t>=8.2 and cy+lh<by+bh:
            d.text((bx+6,cy),"Checking cloudflare.com.. +  443 OPEN",font=fnt(13),fill=TX)
    return img

# ─── SCENE 4: WTOP ────────────────────────────────────────────────────────
def s_wtop(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// Process Manager",["wtop"])
    # Meters
    ma=fade(t,0.4,1.1)
    if ma>0:
        mf=fnt(12)
        d.text((60,225),"CPU",font=mf,fill=tuple(int(c*ma) for c in C))
        d.rectangle([(60,245),(370,252)],fill=BD2)
        d.rectangle([(60,245),(60+int(310*0.046),252)],fill=C)
        d.text((380,242),"4.6%",font=mf,fill=tuple(int(c*ma) for c in G))
        d.text((60,262),"MEM",font=mf,fill=tuple(int(c*ma) for c in G))
        d.rectangle([(60,282),(370,289)],fill=BD2)
        d.rectangle([(60,282),(60+int(310*0.469),289)],fill=G)
        d.text((380,279),"46.9%",font=mf,fill=tuple(int(c*ma) for c in G))
        d.text((60,300),"15275 / 32602 MB  ·  231 procs  ·  00:13:25",font=fnt(11),fill=tuple(int(c*ma) for c in DIM))
    # Keys
    keys=[("↑↓","Nav"),("k","Kill"),("/","Search"),("C M N","Sort"),("q","Quit")]
    kx=60; ky=330
    for i,(k,v) in enumerate(keys):
        ka=fade(t,0.6+i*0.1,1.0+i*0.1)
        if ka>0:
            kf=fnt(12); kw=tw(k,kf)+14
            d.rectangle([(kx,ky),(kx+kw,ky+22)],outline=tuple(int(c*ka) for c in G),width=1)
            d.text((kx+7,ky+4),k,font=kf,fill=tuple(int(c*ka) for c in G))
            kx+=kw+6
            d.text((kx+4,ky+4),v,font=kf,fill=tuple(int(c*ka) for c in DIM))
            kx+=tw(v,kf)+16
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"WTOP — Process Manager")
        d=ImageDraw.Draw(img)
        hf=fnt(11); cf=fnt(12); lh=24
        # HUD
        hy=by+8
        if t>=0.7:
            d.text((bx+8,hy),"CPU",font=hf,fill=A); d.text((bx+36,hy),"4.6%",font=hf,fill=G)
            d.text((bx+95,hy),"MEM",font=hf,fill=A); d.text((bx+125,hy),"46.9%",font=hf,fill=G)
            d.text((bx+200,hy),"15275/32602 MB",font=hf,fill=DIM)
            d.text((bx+bw-130,hy),"00:13:25 · 231",font=hf,fill=DIM)
        hy+=22; d.line([(bx+8,hy),(bx+bw-8,hy)],fill=BD); hy+=4
        # Header
        if t>=1.0:
            d.text((bx+8,hy),"PID   NAME                   CPU%  MEM MB  USER",font=hf,fill=DIM)
            hy+=18; d.line([(bx+8,hy),(bx+bw-8,hy)],fill=(*G,40)); hy+=3
        procs=[
            (1.2,  2780,"AcrobatNotifClient", 0.0, 22.8,"MrX",False),
            (1.35,20124,"brave",              0.1,135.4,"MrX",False),
            (1.5,  7000,"brave",              0.0,662.1,"MrX",False),
            (1.65,12060,"devenv",             0.1,1067.1,"MrX",True),
            (1.8,  2700,"Discord",            0.1,123.0,"MrX",False),
            (1.95, 6480,"copilot-lang-server",0.0,810.9,"MrX",False),
            (2.1, 21120,"codex",              0.0, 72.5,"MrX",False),
            (2.25,15500,"AMDRSSrcExt",        0.0,146.2,"MrX",False),
            (2.4, 15912,"audiodog",           0.1, 11.9, "—", False),
            (2.55,17956,"Discord",            0.0,517.8,"MrX",False),
            (2.7, 18520,"Discord",            0.0, 11.6,"MrX",False),
        ]
        for (st,pid,name,cpu,mem,user,hi) in procs:
            if t>=st and hy+lh<by+bh-4:
                ra=min(1.0,(t-st)/0.2)
                nc=tuple(int(c*ra) for c in G)
                ac=tuple(int(c*ra) for c in A)
                cc=tuple(int(c*ra) for c in C)
                tc=tuple(int(c*ra) for c in TX)
                uc=tuple(int(c*ra) for c in (C if user=="MrX" else DIM))
                if hi:
                    ov2=Image.new('RGBA',img.size,(0,0,0,0))
                    od2=ImageDraw.Draw(ov2)
                    od2.rectangle([(bx+6,hy-1),(bx+bw-6,hy+lh-3)],fill=(0,229,255,18))
                    img=img.convert('RGBA'); img=Image.alpha_composite(img,ov2)
                    img=img.convert('RGB'); d=ImageDraw.Draw(img)
                row=f"{pid:>5}  {name:<22} {cpu:>4.1f}  {mem:>7.1f}  {user}"
                d.text((bx+8,hy),f"{pid:>5}",font=cf,fill=ac)
                d.text((bx+50,hy),name[:22].ljust(22),font=cf,fill=nc)
                d.text((bx+190,hy),f"{cpu:>4.1f}",font=cf,fill=cc)
                d.text((bx+230,hy),f"{mem:>7.1f}",font=cf,fill=tc)
                d.text((bx+310,hy),user,font=cf,fill=uc)
                hy+=lh
        if t>=6.5 and hy+lh<by+bh:
            d.line([(bx+8,hy),(bx+bw-8,hy)],fill=BD); hy+=4
            ov3=Image.new('RGBA',img.size,(0,0,0,0))
            od3=ImageDraw.Draw(ov3)
            od3.rectangle([(bx+8,hy),(bx+bw-8,hy+26)],fill=(*SRF,220),outline=(*G,120))
            od3.text((bx+16,hy+5),"Search: ",font=fnt(13),fill=DIM)
            od3.text((bx+16+tw("Search: ",fnt(13)),hy+5),"brave",font=fnt(13),fill=G)
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov3); img=img.convert('RGB'); d=ImageDraw.Draw(img)
    return img

# ─── SCENE 5: AI ──────────────────────────────────────────────────────────
def s_ai(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// AI Integration",["AI Inside","Your Shell"])
    items=[
        ("cgpt <question>    ","Ask OpenAI / OpenRouter"),
        ("cgpt -o <question> ","Run on local Ollama model"),
        ("cgpt -l            ","List available Ollama models"),
        ("cgpt -sm <model>   ","Set Ollama model to use"),
        ("cgpt -setmodel     ","Set OpenAI / OpenRouter model"),
        ("cgpt -setkey       ","Store API key securely"),
    ]
    iy=285
    for i,(kw,desc) in enumerate(items):
        la=fade(t,0.25+i*0.09,0.65+i*0.09)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in M))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    # Badges
    badges=[("OpenAI GPT-4o",M),("OpenRouter",A),("Ollama Local",G)]
    bx2=60; by2=435
    for i,(nm,col) in enumerate(badges):
        ba=fade(t,1.5+i*0.2,2.0+i*0.2)
        if ba>0:
            pf=fnt(12); pw=tw(nm,pf)+18
            ov=Image.new('RGBA',img.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
            od.rounded_rectangle([(bx2,by2),(bx2+pw,by2+26)],radius=3,
                outline=(*col,int(ba*180)),fill=(*col,int(ba*18)))
            od.text((bx2+9,by2+6),nm,font=pf,fill=(*col,int(ba*255)))
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov)
            img=img.convert('RGB'); d=ImageDraw.Draw(img); bx2+=pw+8
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"xTerminal — cgpt · AI Integration")
        d=ImageDraw.Draw(img)
        mf=fnt(13); lh=23; cy=by+10
        def L(parts,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh: tline(d,bx+8,cy,parts,mf); cy+=lh
        def B(st):
            nonlocal cy
            if t>=st: cy+=lh//2
        L([("> ",DIM),("cgpt ",M),("-setmodel",WH)],0.7)
        L([("Enter model name: ",C),("gpt-4o",WH)],1.1)
        L([("+  Model set to gpt-4o",G)],1.5)
        B(1.5)
        L([("> ",DIM),("cgpt ",M),("What ports should I open for a web server?",WH)],2.0)
        if t>=2.7 and cy+90<by+bh:
            ov4=Image.new('RGBA',img.size,(0,0,0,0)); od4=ImageDraw.Draw(ov4)
            od4.rectangle([(bx+8,cy),(bx+bw-8,cy+86)],fill=(*SRF,220),outline=(*M,80))
            od4.rectangle([(bx+8,cy),(bx+11,cy+86)],fill=(*M,160))
            od4.text((bx+18,cy+4),"◆ GPT-4o",font=fnt(11),fill=M)
            od4.text((bx+18,cy+22),"For a web server open:",font=fnt(13),fill=TX)
            od4.text((bx+18,cy+42),"· Port 80  — HTTP  · Port 443 — HTTPS",font=fnt(13),fill=G)
            od4.text((bx+18,cy+62),"· Port 22  — SSH admin  (use fw -add to apply)",font=fnt(13),fill=A)
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov4)
            img=img.convert('RGB'); d=ImageDraw.Draw(img); cy+=92
        B(3.0)
        L([("> ",DIM),("cgpt ",M),("-sm llama3.2",WH)],3.6)
        L([("+  Ollama model 'llama3.2' is set!",G)],4.0)
        B(4.0)
        L([("> ",DIM),("cgpt -o ",M),("Summarise the latmon command",WH)],4.5)
        L([("◆ Ollama (llama3.2)",G)],5.2)
        L([("latmon is a real-time multi-host latency monitor",G)],5.5)
        L([("with sparklines ▁▂▃▄▅▆▇█, colour-coded tiers,",G)],5.8)
        L([("and cur/avg/min/max/loss stats per host.",G)],6.1)
        B(6.1)
        L([("> ",DIM),("cgpt -l",M)],6.7)
        L([("llama3.2  mistral  codellama  phi3  gemma2",TX)],7.2)
    return img

# ─── SCENE 6: C# RUNNER ───────────────────────────────────────────────────
def s_ccs(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// C# Code Runner & Add-ons",["Extend with","C# Code"])
    items=[
        ("ccs file.cs        ","compile & run in-memory (Roslyn)"),
        ("! -add f.cs -c cmd ","save as persistent add-on"),
        ("! cmd -p args      ","run your add-on"),
        ("! -list            ","list installed add-ons"),
        ("! -del cmd         ","remove an add-on"),
    ]
    iy=285
    for i,(kw,desc) in enumerate(items):
        la=fade(t,0.25+i*0.1,0.65+i*0.1)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in C))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    badges=[("Roslyn In-Memory",C),("Top-Level Statements",G),("Persistent Add-ons",G)]
    bx2=60; by2=430
    for i,(nm,col) in enumerate(badges):
        ba=fade(t,1.5+i*0.2,2.0+i*0.2)
        if ba>0:
            pf=fnt(12); pw=tw(nm,pf)+18
            ov=Image.new('RGBA',img.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
            od.rounded_rectangle([(bx2,by2),(bx2+pw,by2+26)],radius=3,
                outline=(*col,int(ba*180)),fill=(*col,int(ba*18)))
            od.text((bx2+9,by2+6),nm,font=pf,fill=(*col,int(ba*255)))
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov)
            img=img.convert('RGB'); d=ImageDraw.Draw(img); bx2+=pw+8
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"xTerminal — C# Runner · ccs")
        d=ImageDraw.Draw(img)
        KW=(200,90,50); lh=24; cy=by+10; lnf=fnt(11); cf=fnt(13)
        code=[
            (0.7,"1",[("using ",KW),("System",C),(";",DIM)]),
            (0.8,"2",[]),
            (0.9,"3",[("// top-level statements — no class/Main needed",DIM)]),
            (1.1,"4",[("var ",KW),("msg",TX),(" = ",DIM),("args",C),(".Length > 0",DIM)]),
            (1.3,"5",[("    ? ",DIM),("args",C),("[0]",DIM),(" : ",DIM),("\"Hello from C#!\"",A)]),
            (1.6,"6",[]),
            (1.7,"7",[("Console",C),(".WriteLine(",DIM),("msg",TX),(");",DIM)]),
        ]
        for (st,lno,parts) in code:
            if t>=st and cy+lh<by+bh:
                d.text((bx+6,cy),lno.rjust(2),font=lnf,fill=BD)
                cx2=bx+6+30
                for (txt,col2) in parts: d.text((cx2,cy+1),txt,font=cf,fill=col2); cx2+=tw(txt,cf)
                cy+=lh
        cy+=4; d.line([(bx+6,cy),(bx+bw-6,cy)],fill=BD); cy+=6
        mf=fnt(13)
        def L(parts,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh: tline(d,bx+8,cy,parts,mf); cy+=lh
        def B(st):
            nonlocal cy
            if t>=st: cy+=lh//2
        L([("> ",DIM),("ccs ",G),("myaddon.cs",WH)],2.4)
        L([("Hello from C#!",G)],2.9)
        B(2.9)
        L([("> ",DIM),("! ",G),("-add myaddon.cs -c ",WH),("mycmd*My custom addon",A)],3.6)
        L([("+  Add-on 'mycmd' installed successfully",G)],4.2)
        B(4.2)
        L([("> ",DIM),("! -list",G)],4.8)
        L([("mycmd    — My custom addon",TX)],5.3)
        B(5.3)
        L([("> ",DIM),("! mycmd -p ",G),("world",WH)],5.9)
        L([("world",G)],6.4)
    return img

# ─── SCENE 7: SECURITY / PWM ──────────────────────────────────────────────
def s_sec(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// Security Tools",["Security","Built In"])
    items=[
        ("pwm    ","AES-256 + Argon2 encrypted vaults"),
        ("dspoof ","ARP MITM live attack detection"),
        ("shred  ","multi-pass secure file deletion"),
        ("hash   ","MD5 / SHA256 / SHA512 checksums"),
        ("fw     ","firewall rule add/del/list"),
        ("sc     ","service start/stop/restart"),
        ("file   ","magic-number file type checker"),
        ("hex    ","hex dump any file"),
    ]
    iy=275
    for i,(kw,desc) in enumerate(items):
        la=fade(t,0.2+i*0.08,0.6+i*0.08)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in G))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    badges=[("Rijndael AES-256",M),("Argon2 KDF",M),("12-char master pw",DIM)]
    bx2=60; by2=460
    for i,(nm,col) in enumerate(badges):
        ba=fade(t,1.4+i*0.2,1.9+i*0.2)
        if ba>0:
            pf=fnt(12); pw=tw(nm,pf)+18
            ov=Image.new('RGBA',img.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
            od.rounded_rectangle([(bx2,by2),(bx2+pw,by2+26)],radius=3,
                outline=(*col,int(ba*180)),fill=(*col,int(ba*18)))
            od.text((bx2+9,by2+6),nm,font=pf,fill=(*col,int(ba*255)))
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov)
            img=img.convert('RGB'); d=ImageDraw.Draw(img); bx2+=pw+8
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox,60,W//2+20-ox,H-80,"xTerminal — Security · pwm")
        d=ImageDraw.Draw(img)
        mf=fnt(13); lh=24; cy=by+10
        def L(parts,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh: tline(d,bx+8,cy,parts,mf); cy+=lh
        def B(st):
            nonlocal cy
            if t>=st: cy+=lh//2
        L([("> ",DIM),("pwm ",G),("-createv",WH)],0.7)
        L([("Vault name: ",C),("personal",WH)],1.2)
        L([("Master password: ",C),("••••••••••••",DIM)],1.5)
        L([("+  Vault 'personal' created (AES-256 + Argon2)",G)],2.0)
        B(2.0)
        L([("> ",DIM),("pwm ",G),("-addapp",WH)],2.6)
        L([("App: ",C),("GitHub",WH),("  Acct: ",C),("mrx@dev.io",WH)],3.0)
        L([("+  Application added and encrypted",G)],3.5)
        B(3.5)
        L([("> ",DIM),("pwm ",G),("-lista",WH)],4.1)
        if t>=4.6 and cy+80<by+bh:
            ov5=Image.new('RGBA',img.size,(0,0,0,0)); od5=ImageDraw.Draw(ov5)
            od5.rectangle([(bx+8,cy),(bx+bw-8,cy+78)],fill=(*SRF,215),outline=(*BD,150))
            od5.text((bx+16,cy+8),"Application  GitHub",font=fnt(13),fill=TX)
            od5.text((bx+16,cy+30),"Account      mrx@dev.io",font=fnt(13),fill=G)
            od5.text((bx+16,cy+52),"Password     ••••••••••••",font=fnt(13),fill=DIM)
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov5)
            img=img.convert('RGB'); d=ImageDraw.Draw(img); cy+=84
        B(5.0)
        L([("> ",DIM),("dspoof",G)],5.5)
        L([(">>  No ARP spoofing detected",G)],6.1)
        B(6.1)
        L([("> ",DIM),("hash ",G),("-sha256 config.json",WH)],6.7)
        L([("SHA256: e3b0c44298fc1c149afbf4c8996fb924...",C)],7.2)
    return img

# ─── SCENE 8: POWER TOOLS ─────────────────────────────────────────────────
def s_power(t):
    img=new_frame(); d=ImageDraw.Draw(img)
    feat_header(d,img,t,"// Pipe System & Power Tools",["Chain &","Automate"])
    tools=[
        ("bench  ","benchmark commands (min/avg/max/total)"),
        ("watch  ","auto-rerun command on interval"),
        ("fsmon  ","real-time filesystem change monitor"),
        ("snap   ","directory snapshot & diff"),
        ("ctx    ","save/load terminal contexts"),
        ("chain  ","named command chain sequences"),
        ("note   ","persistent terminal notepad"),
        ("uninstall","remove installed applications"),
    ]
    iy=275
    for i,(kw,desc) in enumerate(tools):
        la=fade(t,0.2+i*0.08,0.6+i*0.08)
        if la>0:
            kf=fnt(12)
            d.text((60,iy),kw,font=kf,fill=tuple(int(c*la) for c in G))
            d.text((60+tw(kw,kf),iy),desc,font=kf,fill=tuple(int(c*la) for c in DIM))
        iy+=22
    # Pipe operators
    ops=[("A | B","pipe"),("A && B","if ok"),("A || B","if fail"),("A &","bg")]
    ox2=60; oy=460
    for i,(op,desc) in enumerate(ops):
        oa=fade(t,1.4+i*0.15,1.9+i*0.15)
        if oa>0:
            pf=fnt(12); pw=tw(op,pf)+14
            ov=Image.new('RGBA',img.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
            od.rectangle([(ox2,oy),(ox2+pw,oy+24)],outline=(*G,int(oa*140)),fill=(*G,int(oa*14)))
            od.text((ox2+7,oy+4),op,font=pf,fill=(*G,int(oa*255)))
            ox2+=pw+6
            od.text((ox2+4,oy+4),desc,font=pf,fill=(*DIM,int(oa*255)))
            ox2+=tw(desc,pf)+14
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov)
            img=img.convert('RGB'); d=ImageDraw.Draw(img)
    divider(d)
    ta=fade(t,0.4,1.2)
    if ta>0:
        ox3=int((1-ta)*50)
        img,bx,by,bw,bh=term_win(img,W//2-20+ox3,60,W//2+20-ox3,H-80,"xTerminal — Pipe & Power Tools")
        d=ImageDraw.Draw(img)
        mf=fnt(13); lh=24; cy=by+10
        def L(parts,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh: tline(d,bx+8,cy,parts,mf); cy+=lh
        def B(st):
            nonlocal cy
            if t>=st: cy+=lh//2
        def C2(txt,st):
            nonlocal cy
            if t>=st and cy+lh<by+bh:
                d.text((bx+8,cy),txt,font=fnt(11),fill=DIM); cy+=lh
        C2("# Pipe: list → filter exe files → save",0.7)
        L([("> ",DIM),("ls ",G),("| ",DIM),("cat -s ",G),("exe",WH),(" | ",DIM),("tee ",G),("out.txt",WH)],0.9)
        L([("xTerminal.exe  xt.exe  ccs.exe  wtop.exe",TX)],1.5)
        B(1.5)
        C2("# Benchmark 10 runs",2.1)
        L([("> ",DIM),("bench ",G),("-n 10 ",C),("\"ls -s\"",A)],2.3)
        L([("min:12ms  avg:14ms  max:17ms  total:140ms",G)],2.9)
        B(2.9)
        C2("# Watch filesystem changes",3.4)
        L([("> ",DIM),("fsmon ",G),("-r ",C),("C:\\Projects\\myapp",WH)],3.6)
        L([("CREATED  src\\main.cs     MrX  09:14:32",A)],4.2)
        L([("MODIFIED src\\main.cs     MrX  09:14:41",A)],4.5)
        B(4.5)
        C2("# Save context, create command chain",5.0)
        L([("> ",DIM),("ctx ",G),("save myproject",WH)],5.2)
        L([("+  Context 'myproject' saved",G)],5.7)
        L([("> ",DIM),("chain ",G),("create deploy ",WH),("\"ctx save\" \"bench -n 3 build\"",A)],6.2)
        L([("+  Chain 'deploy' created (2 commands)",G)],6.8)
        L([("> ",DIM),("chain ",G),("run deploy",WH)],7.2)
        L([("ctx save... +   bench -n 3 build... avg:2.2s",G)],7.7)
    return img

# ─── SCENE 9: OUTRO ───────────────────────────────────────────────────────
def s_outro(t):
    img=new_frame()
    g_alpha=0.28+0.12*math.sin(t*1.5)
    img=draw_grid(img,g_alpha)
    d=ImageDraw.Draw(img)
    corners(d)

    # Logo
    lf=fnt(80,True); logo="xTerminal"
    lw=tw(logo,lf)
    la=fade(t,0,0.9)
    if la>0:
        col=tuple(int(c*la) for c in G)
        # Fake multi-layer glow
        for off,aa in [(3,0.06),(2,0.12),(1,0.20)]:
            gc=tuple(int(c*la*aa) for c in G)
            for dx,dy in [(-off,0),(off,0),(0,-off),(0,off)]:
                d.text((W//2-lw//2+dx,H//2-210+dy),logo,font=lf,fill=gc)
        d.text((W//2-lw//2,H//2-210),logo,font=lf,fill=col)

    # Tagline
    tag="Linux-like  ·  Open Source  ·  MIT License  ·  .NET 10"
    tf=fnt(14); tw2=tw(tag,tf)
    ta2=fade(t,0.8,1.6)
    if ta2>0:
        d.text((W//2-tw2//2,H//2-100),tag,font=tf,fill=tuple(int(c*ta2) for c in DIM))

    # GitHub
    gh="github.com/0x78654C/xTerminal"
    gf=fnt(16); gw=tw(gh,gf)
    ga=fade(t,1.3,2.1)
    if ga>0:
        d.text((W//2-gw//2,H//2-68),gh,font=gf,fill=tuple(int(c*ga) for c in C))

    # Stats
    stats=[("100+","Commands","#00ff88"),("AI","Integrated","#d466ff"),
           ("C#","Scriptable","#00e5ff"),("v3.0","Latest","#ffb300")]
    sw2=len(stats)*175; sx=W//2-sw2//2
    for i,(v,l,ch) in enumerate(stats):
        sa=fade(t,1.8+i*0.22,2.5+i*0.22)
        if sa>0:
            col=tuple(int(int(ch.lstrip('#')[j:j+2],16)*sa) for j in (0,2,4))
            vf=fnt(40,True); lf2=fnt(11)
            vw=tw(v,vf); lw2=tw(l,lf2)
            cx=sx+i*175+87
            d.text((cx-vw//2,H//2-14),v,font=vf,fill=col)
            d.text((cx-lw2//2,H//2+36),l,font=lf2,fill=tuple(int(c*sa) for c in DIM))

    # Chips
    chips=["* Star on GitHub","# .NET 10","@ MIT License","~ Linux-style"]
    cx2=W//2-len(chips)*85; cy=H//2+65
    for i,chip in enumerate(chips):
        ca=fade(t,2.9+i*0.18,3.5+i*0.18)
        if ca>0:
            pf=fnt(12); pw=tw(chip,pf)+18
            ov=Image.new('RGBA',img.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
            od.rounded_rectangle([(cx2,cy),(cx2+pw,cy+26)],radius=3,
                outline=(*G,int(ca*120)),fill=(*G,int(ca*12)))
            od.text((cx2+9,cy+6),chip,font=pf,fill=(*G,int(ca*255)))
            img=img.convert('RGBA'); img=Image.alpha_composite(img,ov)
            img=img.convert('RGB'); d=ImageDraw.Draw(img); cx2+=pw+8
    return img

# ─── MAIN ─────────────────────────────────────────────────────────────────
SCENES=[
    (s_intro,     6.0),
    (s_shell,     8.0),
    (s_net,       9.0),
    (s_xt,        9.0),
    (s_wtop,      8.0),
    (s_ai,        8.0),
    (s_ccs,       8.0),
    (s_sec,       8.0),
    (s_power,     8.0),
    (s_outro,     6.0),
]
N=len(SCENES)

def main():
    ffmpeg = shutil.which("ffmpeg")

    if not ffmpeg:
        print("ERROR: ffmpeg.exe was not found in PATH.")
        print("Install FFmpeg, then add its bin folder to PATH.")
        print("Example PATH folder: C:\\ffmpeg\\bin")
        sys.exit(1)

    total     = sum(int(d * FPS) for _, d in SCENES)
    total_dur = sum(d for _, d in SCENES)
    print(f"Rendering {total} frames ({total_dur:.0f}s @ {FPS}fps)")
    print(f"Output: {OUT}")
    sys.stdout.flush()

    print("Generating audio track...")
    sys.stdout.flush()
    audio_path = os.path.join(tempfile.gettempdir(), "xtermvid_audio.wav")
    save_wav(generate_audio(total_dur), audio_path)

    cmd = [
        ffmpeg,
        "-y",
        "-f", "rawvideo",
        "-vcodec", "rawvideo",
        "-s", f"{W}x{H}",
        "-pix_fmt", "rgb24",
        "-r", str(FPS),
        "-i", "pipe:0",
        "-i", audio_path,
        "-map", "0:v:0",
        "-map", "1:a:0",
        "-c:v", "libx264",
        "-preset", "fast",
        "-crf", "18",
        "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        "-b:a", "192k",
        "-shortest",
        "-movflags", "+faststart",
        OUT,
    ]

    proc = subprocess.Popen(
        cmd,
        stdin=subprocess.PIPE,
        stderr=subprocess.PIPE
    )

    fn = 0

    try:
        for si, (sfunc, dur) in enumerate(SCENES):
            nf = int(dur * FPS)

            for f in range(nf):
                t = f / FPS
                img = sfunc(t).convert("RGB")
                d = ImageDraw.Draw(img)
                hud(d, si, N, t, dur)

                proc.stdin.write(img.tobytes())

                fn += 1
                if fn % FPS == 0:
                    pct = fn / total * 100
                    print(
                        f"\r  {pct:5.1f}%  scene {si + 1}/{N}  frame {fn}/{total}  ",
                        end=""
                    )
                    sys.stdout.flush()

        proc.stdin.close()
        stderr = proc.stderr.read().decode(errors="ignore")
        code = proc.wait()

        if code != 0:
            print("\nERROR: FFmpeg failed.")
            print(stderr[-3000:])
            sys.exit(code)

        print(f"\nDone: {OUT}")

    except BrokenPipeError:
        stderr = proc.stderr.read().decode(errors="ignore")
        print("\nERROR: FFmpeg pipe broke.")
        print(stderr[-3000:])
        sys.exit(1)

    finally:
        try:
            os.remove(audio_path)
        except OSError:
            pass

if __name__=="__main__":
    main()
