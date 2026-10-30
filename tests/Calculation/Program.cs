using FluentPrayerTimes;
using System.Text.Json;
var output=args.Length>0?args[0]:"calculation-results.json";
var results=new List<object>();int fails=0;int checks=0;
void Check(string name,bool ok,object? evidence=null){checks++;if(!ok)fails++;results.Add(new {name,passed=ok,evidence});}
foreach(var place in new[]{("NewBorg",30.8619,29.5845),("Cairo",30.0444,31.2357)}) {
 var row=SolarCalc.Compute(new DateTime(2026,10,30),place.Item2,place.Item3,2,5,null,null,1,3);
 Console.WriteLine(place.Item1+" "+string.Join(" ",Times.Keys.Select(k=>row![k])));
}
var borg=SolarCalc.Compute(new DateTime(2026,10,30),30.8619,29.5845,2,5,null,null,1,3)!;
Check("Original Wathkkir New Borg screenshot 30 Oct 2026",string.Join(" ",Times.Keys.Select(k=>borg[k]))=="04:46 06:14 11:45 14:52 17:16 18:35");
var s=new Settings();
var t=new Dictionary<string,string>{{"Fajr","04:00"},{"Sunrise","05:30"},{"Dhuhr","12:00"},{"Asr","15:30"},{"Maghrib","18:00"},{"Isha","19:30"}};
var day=new DateTime(2026,10,30);
foreach(var idx in new[]{0,2,3,4,5}){
 var at=Times.At(day,t[Times.Keys[idx]]);var iq=at.AddMinutes(s.IqamaFor(idx));
 Check($"{idx}: second before adhan",Times.State(at.AddSeconds(-1),t,t,s)?.Phase==Times.Phase.Next);
 Check($"{idx}: adhan begins",Times.State(at,t,t,s)?.Phase==Times.Phase.Adhan);
 Check($"{idx}: adhan last second",Times.State(at.AddSeconds(59),t,t,s)?.Phase==Times.Phase.Adhan);
 Check($"{idx}: iqama starts after minute",Times.State(at.AddMinutes(1),t,t,s)?.Phase==Times.Phase.Iqama);
 Check($"{idx}: iqama last second",Times.State(iq.AddSeconds(-1),t,t,s)?.Left==TimeSpan.FromSeconds(1));
 Check($"{idx}: iqama arrived",Times.State(iq,t,t,s)?.Phase==Times.Phase.IqamaNow);
 Check($"{idx}: return to next",Times.State(iq.AddMinutes(1),t,t,s)?.Phase==Times.Phase.Next);
}
Check("Sunrise no adhan",Times.State(day.AddHours(5.5),t,t,s)?.Phase==Times.Phase.Next);
Check("Tomorrow Fajr rollover",Times.State(day.AddHours(23.99),t,t,s) is {Idx:0,Tomorrow:true});
Check("Countdown rounds up final seconds",Times.FmtHM(TimeSpan.FromSeconds(1))=="00:01");
Check("Negative countdown clamped",Times.FmtMS(TimeSpan.FromSeconds(-1))=="00:00");
foreach(var zone in new[]{("Africa/Cairo",new DateTime(2026,10,29),3d),("Africa/Cairo",new DateTime(2026,10,30),2d),("Africa/Cairo",new DateTime(2026,4,23),2d),("Africa/Cairo",new DateTime(2026,4,24),3d),("America/New_York",new DateTime(2026,11,1),-5d),("Asia/Kolkata",day,5.5d)})Check("zone "+zone,Times.OffsetHours(new Settings{Location=new Loc{Cc="US"}},zone.Item1,zone.Item2)==zone.Item3);
// Write local calculator matrix to compare with downloaded independent Aladhan responses.
var matrix=new List<object>();
foreach(var loc in new[]{("Cairo",30.0444,31.2357,2d),("NewYork",40.7128,-74.006,-4d),("London",51.5074,-0.1278,0d),("Sydney",-33.8688,151.2093,11d),("OsloSummer",59.9139,10.7522,2d)})
foreach(var method in new[]{1,2,3,5,9,99})foreach(var school in new[]{1,2})foreach(var high in new[]{0,1,2,3}){
 var date=loc.Item1=="OsloSummer"?new DateTime(2026,6,21):day;
 var row=SolarCalc.Compute(date,loc.Item2,loc.Item3,loc.Item4,method,method==99?18:null,method==99?17:null,school,high);
 matrix.Add(new{location=loc.Item1,date=date.ToString("dd-MM-yyyy"),lat=loc.Item2,lon=loc.Item3,offset=loc.Item4,method,school,high,timings=row});
 if(row!=null) Check("valid solar time matrix",row.Values.All(v=>TimeSpan.TryParse(v,out var ts)&&ts>=TimeSpan.Zero&&ts<TimeSpan.FromDays(1)));
}
var cal = new System.Globalization.UmAlQuraCalendar();
Check("Hijri 30 Oct 2026 UAQ reference",cal.GetYear(day)==1448 && cal.GetMonth(day)==5 && cal.GetDayOfMonth(day)==19);
foreach(var date in Enumerable.Range(0,366).Select(i=>new DateTime(2026,1,1).AddDays(i))) {
 var back=cal.ToDateTime(cal.GetYear(date),cal.GetMonth(date),cal.GetDayOfMonth(date),0,0,0,0);
 Check("Hijri Gregorian round trip "+date.ToString("yyyy-MM-dd"),back==date);
}
File.WriteAllText(output,JsonSerializer.Serialize(new{checks,fails,results,matrix},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"{checks} checks; {fails} failed");return fails>0?1:0;
