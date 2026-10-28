"""Regenerate editable native source from the audited reconstructed classes.
Removes only original compiler-generated XAML glue; the Windows SDK generates new glue.
"""
from pathlib import Path
import re
root=Path(__file__).resolve().parent
source=root.parent/'src'/'FluentPrayerTimes'
for p in source.glob('*.cs'):
 if p.stem=='Program':continue
 s=p.read_text()
 if p.stem in ['App','MainWindow','WidgetWindow']:
  s=re.sub(r'^using FluentPrayerTimes\.FluentPrayerTimes_XamlTypeInfo;\n','',s,flags=re.M)
  s=re.sub(r'^using WinRT\.FluentPrayerTimesVtableClasses;\n','',s,flags=re.M)
  s=re.sub(r'^\[WinRT(?:RuntimeClassName|ExposedType).*\]\n','',s,flags=re.M)
  s=s.replace('public class App : Application, IXamlMetadataProvider','public partial class App : Application')
  s=s.replace('public sealed class '+p.stem+' : Window, IComponentConnector','public sealed partial class '+p.stem+' : Window')
  # Generated members are marked by GeneratedCode and followed by a field, method, or property.
  while True:
   m=re.search(r'\n\s*\[GeneratedCode\([^\n]+\)\]\s*\n(?:\s*\[[^\n]+\]\s*\n)*',s)
   if not m:break
   start=m.start();member=m.end();semi=s.find(';',member);brace=s.find('{',member)
   if semi>=0 and (brace<0 or semi<brace):end=semi+1
   else:
    # Generated glue bodies contain only conventional C# literals and no nested interpolated braces.
    depth=1;end=brace+1
    while depth:
     if s[end]=='{':depth+=1
     elif s[end]=='}':depth-=1
     end+=1
   s=s[:start]+s[end:]
 (root/(p.stem+'.cs')).write_text(s)
for p in (root.parent/'xaml').glob('*.xaml'):
 s=p.read_text()
 s=re.sub(r'<Color ([^>]*?)Value="([^"]+)"\s*/>',r'<Color \1>\2</Color>',s)
 (root/p.name).write_text(s)
