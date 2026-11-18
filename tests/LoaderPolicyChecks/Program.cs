using MegaCrit.Sts2.Core.Debug;
using ShoujoKagekiAijoKaren.Loader;
var checks=0;
void Expect(int major,int minor,int patch,string expected) {
 var actual=Bootstrap.SelectVariant(new SemanticVersion(major,minor,patch));
 if(actual!=expected)throw new InvalidOperationException($"{major}.{minor}.{patch}: expected {expected}, got {actual}");
 checks++;
}
void Reject(int major,int minor,int patch) {
 try { Bootstrap.SelectVariant(new SemanticVersion(major,minor,patch)); }
 catch(NotSupportedException) { checks++;return; }
 throw new InvalidOperationException("Expected rejection of an incompatible embedded base");
}
Expect(0,107,1,"Stable");Expect(0,113,0,"Beta");Reject(0,107,0);
#if KAREN_ANDROID
Reject(0,111,1);Reject(0,113,1);Reject(0,114,0);
#else
Expect(0,107,2,"Stable");Expect(0,110,0,"Stable");
Expect(0,111,0,"Main");Expect(0,111,1,"Main");Expect(0,112,0,"Main");
Expect(0,113,1,"Beta");Expect(0,114,0,"Beta");Expect(1,0,0,"Beta");
#endif
Console.WriteLine($"Loader policy checks passed: {checks}");
