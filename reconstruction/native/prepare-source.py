"""Regenerate editable native source from the audited reconstructed classes.
Removes only original compiler-generated XAML glue; the Windows SDK generates new glue.
"""
from pathlib import Path
import re
root=Path(__file__).resolve().parent
source=root.parent/'src'/'FluentPrayerTimes'
for p in source.glob('*.cs'):
 if p.stem=='Program':continue
 s=p.read_text(encoding="utf-8")
 s=re.sub(r'(?<![\w.])Colors\.', 'Microsoft.UI.Colors.', s)
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
 if p.stem == 'App':
  s=s.replace('InitializeComponent();', 'if (Environment.GetEnvironmentVariable("FPT_XAML_DIAGNOSTICS") == "1") {\n            System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs? last = null;\n            AppDomain.CurrentDomain.FirstChanceException += (sender, e) => { try { if (e.Exception.GetType().Name.Contains("Xaml") || e.Exception is NullReferenceException) { Directory.CreateDirectory(AppData); File.AppendAllText(Path.Combine(AppData, "xaml-diagnostics.log"), e.Exception.ToString()+"\\n"); } } catch {} };\n            DebugSettings.XamlResourceReferenceFailed += (sender, e) => { try { Directory.CreateDirectory(AppData); File.AppendAllText(Path.Combine(AppData, "xaml-diagnostics.log"), e.Message+"\\n"); } catch {} };\n        }\n        InitializeComponent();')
 if p.stem == 'MainWindow':
  s=s.replace('InitializeComponent();', 'if (Environment.GetEnvironmentVariable("FPT_XAML_DIAGNOSTICS") == "1") { try { var markup=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"MainWindow.diagnostic.xaml")); Microsoft.UI.Xaml.Markup.XamlReader.Load(markup); } catch(Exception ex) { Directory.CreateDirectory(App.AppData); File.AppendAllText(Path.Combine(App.AppData,"xaml-diagnostics.log"),"XamlReader: "+ex.ToString()+"\\n"); } }\n        InitializeComponent();')
 (root/(p.stem+'.cs')).write_text(s, encoding="utf-8")
for p in (root.parent/'xaml').glob('*.xaml'):
 s=p.read_text(encoding="utf-8")
 s=re.sub(r'(?<![\w.])Colors\.', 'Microsoft.UI.Colors.', s)
 s=re.sub(r'<Color ([^>]*?)Value="([^"]+)"\s*/>',r'<Color \1>\2</Color>',s)
 (root/p.name).write_text(s, encoding="utf-8")

# Runtime-only parser diagnostic, not the app's loaded XAML.
s=(root/'MainWindow.xaml').read_text(encoding='utf-8')
s=re.sub(r' x:Class="[^"]*"','',s)
s=re.sub(r' (?:SelectionChanged|Click|Checked|Toggled|TextChanged|QuerySubmitted|SuggestionChosen)="[^"]*"','',s)
(root/'MainWindow.diagnostic.xaml').write_text(s,encoding='utf-8')
