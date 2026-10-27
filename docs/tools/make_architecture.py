from PIL import Image, ImageDraw, ImageFont
import os

W, H = 1600, 860
BG = (18, 21, 26)
PANEL = (26, 31, 39)
PANEL2 = (33, 40, 50)
LINE = (63, 74, 90)
TEXT = (232, 236, 243)
MUTED = (147, 160, 180)
ACCENT = (240, 169, 44)
OK = (70, 192, 122)
BLUE = (86, 156, 214)

img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

FONT_DIR = "C:/Windows/Fonts"
def font(name, size):
    for cand in name:
        p = os.path.join(FONT_DIR, cand)
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()

bold = font(["msyhbd.ttc", "msyh.ttf"], 34)
h2 = font(["msyhbd.ttc", "msyh.ttf"], 24)
body = font(["msyh.ttc", "msyhbd.ttc"], 19)
small = font(["msyh.ttc"], 17)
mono = font(["consola.ttf"], 15)

def rounded(box, fill, outline, radius=14, width=2):
    d.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)

def center_text(x, y, s, f, fill):
    w = d.textlength(s, font=f)
    d.text((x - w / 2, y), s, font=f, fill=fill)

d.text((60, 46), "AR 巡检问题标注 · 系统架构", font=bold, fill=TEXT)
d.text((60, 92), "Unity 上报 → Go 持久化 → React 管理，三端共用同一份数据", font=small, fill=MUTED)

# boxes
unity = (70, 180, 530, 560)
go = (660, 180, 930, 560)
react = (1060, 180, 1520, 560)
db = (660, 640, 930, 780)

rounded(unity, PANEL, LINE)
rounded(go, PANEL, LINE)
rounded(react, PANEL, LINE)
rounded(db, PANEL2, LINE)

d.text((96, 202), "Unity 客户端", font=h2, fill=TEXT)
d.text((96, 238), "2022.3 LTS · Built-in RP", font=small, fill=MUTED)
for i, t in enumerate([
    "· 虚拟机房场景漫游",
    "· 射线点击放置标记",
    "· 表单校验并上报",
    "· 进场景恢复历史标记",
    "· 点击标记查看详情",
]):
    d.text((96, 288 + i * 40), t, font=body, fill=TEXT)

d.text((686, 202), "Go 后端", font=h2, fill=TEXT)
d.text((686, 238), "1.27 · net/http", font=small, fill=MUTED)
for i, t in enumerate([
    "· 4 个 REST 接口",
    "· 参数校验 / 枚举",
    "· CORS / 请求日志",
    "· 优雅退出",
]):
    d.text((686, 288 + i * 40), t, font=body, fill=TEXT)

d.text((1086, 202), "React 管理端", font=h2, fill=TEXT)
d.text((1086, 238), "React 19 · TypeScript · Vite", font=small, fill=MUTED)
for i, t in enumerate([
    "· 问题列表 / 状态筛选",
    "· 改状态：open → in_progress",
    "   → resolved",
    "· 删除记录（二次确认）",
    "· 后端不可用：错误横幅",
]):
    d.text((1086, 288 + i * 40), t, font=body, fill=TEXT)

center_text(795, 660, "SQLite（issues.db）", h2, TEXT)
center_text(795, 704, "重启不丢数据", small, MUTED)
center_text(795, 736, "modernc.org/sqlite · 纯 Go", small, MUTED)

# arrows
def arrow(x1, y1, x2, y2, color=ACCENT, dash=False):
    if dash:
        d.line((x1, y1, x2, y2), fill=color, width=2)
    else:
        d.line((x1, y1, x2, y2), fill=color, width=3)
    # head
    if x2 > x1:
        d.polygon([(x2, y2), (x2 - 14, y2 - 7), (x2 - 14, y2 + 7)], fill=color)
    elif x2 < x1:
        d.polygon([(x2, y2), (x2 + 14, y2 - 7), (x2 + 14, y2 + 7)], fill=color)
    else:
        d.polygon([(x2, y2), (x2 - 7, y2 - 14), (x2 + 7, y2 - 14)], fill=color)

arrow(530, 300, 660, 300, ACCENT)
center_text(595, 258, "POST /api/issues", mono, ACCENT)

arrow(660, 430, 530, 430, MUTED)
center_text(595, 444, "错误信息", small, MUTED)

arrow(1060, 300, 930, 300, BLUE)
center_text(995, 246, "GET / PATCH", mono, BLUE)
center_text(995, 270, "DELETE", mono, BLUE)

arrow(930, 430, 1060, 430, MUTED)
center_text(995, 444, "JSON", small, MUTED)

arrow(795, 560, 795, 640, OK)
d.text((806, 582), "读写", font=small, fill=OK)

d.text((60, 806), "同一份数据：Unity 上报的坐标、优先级，网页端立刻可见；修改状态同样落到同一个库。", font=small, fill=MUTED)

os.makedirs("D:/springboot/AR/docs/images", exist_ok=True)
out = "D:/springboot/AR/docs/images/architecture.png"
img.save(out, "PNG", optimize=True)
print(out, os.path.getsize(out) // 1024, "KB")

