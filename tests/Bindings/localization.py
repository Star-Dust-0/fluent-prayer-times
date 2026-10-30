from pathlib import Path
import re,json,xml.etree.ElementTree as E
root=Path(__file__).resolve().parents[2]/'src/FluentPrayerTimes'
source=(root/'Strings.cs').read_text(encoding='utf-8')
entries=[]
for key,n,body in re.findall(r'\["((?:[^"\\]|\\.)*)"\]\s*= new string\[(\d+)\]\s*\{([^}]*)\}',source):
 values=[json.loads('"'+x+'"') for x in re.findall(r'"((?:[^"\\]|\\.)*)"',body)]
 entries.append(dict(key=json.loads('"'+key+'"'),columns=int(n),english=values[0] if values else '',values=values))
keys={x['key'] for x in entries};missing=[]
for f in root.glob('*.cs'):
 if f.name in ['Strings.cs','SelfTest.cs']:continue
 for text in re.findall(r'L\.T\("((?:[^"\\]|\\.)*)"\)',f.read_text(encoding='utf-8')):
  text=json.loads('"'+text+'"')
  if re.search('[\u0600-\u06ff]',text) and text not in keys:missing.append(dict(file=f.name,text=text))
print(json.dumps(dict(entries=len(entries),missing_translation_keys=missing,english_only_fallback=[x['key'] for x in entries if x['columns']<10],arabic_left_in_english=[x for x in entries if re.search('[\u0600-\u06ff]',x['english'])],intentional_exceptions=['Arabic religious texts/azkar','language names','app and framework proper names','coordinate labels'],note='A key existing does not prove translation quality or runtime refresh. Non-English fallback is reported, not silently passed.'),ensure_ascii=True,indent=2))
raise SystemExit(bool(missing))
