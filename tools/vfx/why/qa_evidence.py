"""Make labelled before/after contact sheets for inspection of the captured MCP evidence."""
import argparse, json, pathlib
from PIL import Image, ImageDraw, ImageFont

p=argparse.ArgumentParser();p.add_argument('root',type=pathlib.Path);args=p.parse_args()
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)
sheets=0
for branch in ['stable','beta']:
    folder=args.root/branch
    results=json.loads((folder/'results.json').read_text(encoding='utf-8'))
    for group in range(0,len(results),3):
        cases=results[group:group+3]
        sheet=Image.new('RGB',(1280,len(cases)*390),(15,23,31))
        draw=ImageDraw.Draw(sheet)
        for row,result in enumerate(cases):
            name=result['case']
            for col,moment in enumerate(['before','after']):
                image=Image.open(folder/name/(moment+'.png')).convert('RGB').resize((640,360))
                sheet.paste(image,(col*640,row*390+30))
                draw.text((col*640+8,row*390+5),f'{branch} / {name} / {moment}',font=font,fill=(255,216,160))
        sheet.save(folder/f'contact-{group//3}.jpg',quality=95)
        sheets+=1
print(f'Created {sheets} before/after contact sheets.')
