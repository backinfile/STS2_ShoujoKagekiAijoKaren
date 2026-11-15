namespace MegaCrit.Sts2.Core.Debug {
public sealed class SemanticVersion(int major,int minor,int patch) : IComparable<SemanticVersion> {
 private readonly Version value=new(major,minor,patch);
 public int CompareTo(SemanticVersion? other)=>value.CompareTo(other?.value);
 public override string ToString()=>value.ToString();
}}
namespace MegaCrit.Sts2.Core.Logging { public enum LogType { Generic } public sealed class Logger(string name,LogType type) { public void Warn(string message) { _=name;_=type; } } }
namespace MegaCrit.Sts2.Core.Modding { [AttributeUsage(AttributeTargets.Class)] public sealed class ModInitializerAttribute(string name):Attribute { public string Name {get;}=name; } }
namespace ShoujoKageki.Core.Loader { internal static class CharacterLoader { internal static void Initialize(string id,string entry,Func<MegaCrit.Sts2.Core.Debug.SemanticVersion,string> select,MegaCrit.Sts2.Core.Logging.Logger log)=>throw new NotSupportedException(); } }
