using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
class ProbeContext : AssemblyLoadContext {
 readonly string dir; public ProbeContext(string name,string dir):base(name,true){this.dir=dir;}
 protected override Assembly? Load(AssemblyName n){var p=Path.Combine(dir,n.Name+".dll"); if(n.Name=="System.Private.CoreLib"||n.Name=="System.Runtime"||n.Name?.StartsWith("System.")==true)return null;return File.Exists(p)?LoadFromAssemblyPath(p):null;}
}
class Program {
 static int Main(string[] args){
 var a=Path.GetFullPath(args[0]); var b=Path.GetFullPath(args[1]); var ca=new ProbeContext("original",Path.GetDirectoryName(a)!); var cb=new ProbeContext("rebuilt",Path.GetDirectoryName(a)!); var aa=ca.LoadFromAssemblyPath(a);var ab=cb.LoadFromAssemblyPath(b);
 int n=0; int exceptions=0; var errors=new List<string>();
 string Run(Assembly asm,string type,string method,object?[] v){try {var t=asm.GetType("FluentPrayerTimes."+type,true)!;var r=t.GetMethod(method,BindingFlags.Public|BindingFlags.Static)!.Invoke(null,v);return JsonSerializer.Serialize(r);}catch(Exception e){return "EXCEPTION:"+(e.InnerException??e).GetType().FullName+":"+(e.InnerException??e).Message;}}
 void Check(string type,string method,params object?[] v){n++;var x=Run(aa,type,method,v);var y=Run(ab,type,method,v);if(x.StartsWith("EXCEPTION:")||y.StartsWith("EXCEPTION:")){exceptions++;errors.Add($"EXCEPTION {type}.{method}: {x} / {y}");}else if(x!=y)errors.Add($"{type}.{method} {string.Join(",",v.Select(z=>z?.ToString()))}: {x} != {y}");}
 foreach(var date in new[]{new DateTime(2026,1,1),new DateTime(2026,3,20),new DateTime(2026,6,21),new DateTime(2026,10,23),new DateTime(2026,12,31)})
 foreach(var loc in new[]{(30.0444,31.2357,2.0),(21.4225,39.8262,3.0),(40.7128,-74.006,-5.0),(51.5074,-0.1278,0.0),(69.6492,18.9553,1.0),(-33.8688,151.2093,11.0)})
 foreach(var method in new[]{1,2,3,4,5,9,99})foreach(var asr in new[]{1,2})foreach(var high in new[]{0,1,2,3})Check("SolarCalc","Compute",date,loc.Item1,loc.Item2,loc.Item3,method,method==99?19.5:null,method==99?17.5:null,asr,high);
 foreach(var method in Enumerable.Range(-1,102)) Check("SolarCalc","Supports",method);
 foreach(var sec in new[]{-3600.0,-1,0,1,30,59,60,61,3599,3600,86399,86400})foreach(var m in new[]{"FmtHM","FmtMS","FmtMinutes","Fmt"})Check("Times",m,TimeSpan.FromSeconds(sec));

 object SettingsFor(Assembly asm, string json){return JsonSerializer.Deserialize(json,asm.GetType("FluentPrayerTimes.Settings",true)!)!;}
 string ProbeState(Assembly asm,DateTime now,string json){try {var t=asm.GetType("FluentPrayerTimes.Times",true)!;var dic=new Dictionary<string,string>{{"Fajr","04:00"},{"Sunrise","05:30"},{"Dhuhr","12:00"},{"Asr","15:30"},{"Maghrib","18:00"},{"Isha","19:30"}};return JsonSerializer.Serialize(t.GetMethod("State")!.Invoke(null,new object?[]{now,dic,dic,SettingsFor(asm,json)}));}catch(Exception e){return "EXCEPTION:"+(e.InnerException??e).ToString();}}
 foreach(var json in new[]{"{}","{\"IqamaMin\":[30,0,25,20,10,40]}","{\"IqamaMin\":null}","{\"IqamaMin\":[-1,0,1,181,999,20]}"}){
 for(int sec=0;sec<86400;sec+=17){n++;var date=new DateTime(2026,10,9).AddSeconds(sec);var x=ProbeState(aa,date,json);var y=ProbeState(ab,date,json);if(x.StartsWith("EXCEPTION:")||y.StartsWith("EXCEPTION:")){exceptions++;errors.Add("State exception "+x+" / "+y);}else if(x!=y)errors.Add($"State {date}: {x} != {y}");}
 foreach(var asm in new[]{aa,ab}) {var settings=SettingsFor(asm,json);for(int i=-2;i<9;i++){n++;var actual=settings.GetType().GetMethod("IqamaFor")!.Invoke(settings,new object[]{i}); var other=SettingsFor(asm==aa?ab:aa,json);var expected=other.GetType().GetMethod("IqamaFor")!.Invoke(other,new object[]{i});if(!Equals(actual,expected))errors.Add("IqamaFor mismatch");}}
 }
 var result=new {checks=n,exceptions,failed=errors.Count,errors};Console.WriteLine(JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));return errors.Count==0?0:1;
 }
}
