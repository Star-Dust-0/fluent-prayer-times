namespace FluentPrayerTimes {
public static class L { public static string T(string s)=>s; public static bool IsArabic=>false;public static string CountryName(string c,string a)=>c; }
internal static class LocationDetection { internal record Fix(double Lat,double Lon,string City="",string Country="",string Cc="",string Source="windows");internal static void Log(string s,string t){} internal static Task<Fix?> DetectAsync(Func<CancellationToken,Task<Fix?>> f,HttpClient h)=>Task.FromResult<Fix?>(null); }
}
namespace Windows.Devices.Geolocation {
public enum GeolocationAccessStatus {Allowed} public enum PositionAccuracy { Default }
public class Geolocator {public PositionAccuracy DesiredAccuracy{get;set;} public static Fake<GeolocationAccessStatus> RequestAccessAsync()=>new(); public Fake<Position> GetGeopositionAsync(TimeSpan a,TimeSpan b)=>new(); }
public class Fake<T> { public Task<T> AsTask(CancellationToken t)=>throw new NotSupportedException();}
public class Position {public Coord Coordinate=>new();} public class Coord {public Point Point=>new();}public class Point {public GeoPosition Position=>new();} public class GeoPosition {public double Latitude=>0;public double Longitude=>0;}
}
