from pathlib import Path
import xml.etree.ElementTree as E
import re,json
root=Path(__file__).resolve().parents[2]/'src/FluentPrayerTimes'
actions={'Button','HyperlinkButton','ToggleButton','ToggleSwitch','RadioButton','ComboBox','TextBox','ListView','NavigationView','Expander'}
events={'Click','Command','Toggled','Checked','SelectionChanged','KeyDown','TextChanged','ValueChanged','Expanded','Collapsed'}
rows=[]; failures=[]
for f in root.glob('*.xaml'):
 code=f.with_suffix('.cs').read_text(encoding="utf-8") if f.with_suffix('.cs').exists() else ''
 for el in E.parse(f).iter():
  tag=el.tag.split('}')[-1]
  if tag not in actions:continue
  name=el.get('{http://schemas.microsoft.com/winfx/2006/xaml}Name','')
  bound={k:v for k,v in el.attrib.items() if k in events or k=='NavigateUri'}
  if name:
   bound.update(dict(re.findall(r'\b'+re.escape(name)+r'\.(\w+)\s*\+=\s*(\w+)',code)))
  native=tag in {'Expander','TextBox','HyperlinkButton'} or (tag=='NavigationView' and 'SelectionChanged' in bound)
  # Input TextBoxes are consumed by Apply/Search; hyperlinks can be assigned at runtime.
  if tag=='HyperlinkButton' and 'NavigateUri' not in bound and name:
   native=bool(re.search(r'\b'+name+r'\b',code) and 'NavigateUri =' in code)
  if not bound and not native:failures.append(str(f.name)+':'+name+':'+tag)
  for event,handler in bound.items():
   if event not in {'NavigateUri','Command'} and not re.search(r'\b'+re.escape(handler)+r'\s*\(',code): failures.append('Missing handler '+handler)
  rows.append(dict(file=f.name,type=tag,id=name,bindings=bound,native=native))
# Every named event-shaped handler must have a reference beyond its declaration.
for f in root.glob('*.cs'):
 code=f.read_text(encoding="utf-8")
 for handler in re.findall(r'\b(?:void|Task)\s+(\w+_(?:Click|Changed|Toggled|Checked|Selected|KeyDown|PointerPressed|PointerMoved|PointerReleased))\s*\(',code):
  corpus=code+(f.with_suffix('.xaml').read_text(encoding="utf-8") if f.with_suffix('.xaml').exists() else '')
  if len(re.findall(r'\b'+handler+r'\b',corpus))<2:failures.append('Unreferenced '+f.name+':'+handler)
report={'xaml_controls':len(rows),'controls':rows,'failures':failures,'code_created_files':[f.name for f in root.glob('*.cs') if re.search(r'new (Button|ComboBox|NumberBox|ToggleSwitch|ColorPicker)',f.read_text(encoding="utf-8"))]}
print(json.dumps(report,ensure_ascii=True,indent=2));raise SystemExit(bool(failures))
