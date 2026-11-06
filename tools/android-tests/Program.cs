using ShoujoKagekiAijoKaren.src.Core.Utils;

int count = 0;
void Check(bool result, string label)
{
    if (!result) throw new Exception(label);
    Console.WriteLine("PASS " + label); count++;
}
var g = new KarenTapGesture();
Check(!g.End(0, 20, 20, true), "release without press does not open a pile");
g.Begin(0, 20, 20, true);
Check(g.End(0, 22, 22, true), "single-finger tap opens once");
Check(!g.End(0, 22, 22, true), "duplicate release does not toggle again");
g.Begin(0, 20, 20, true); g.Move(0, 70, 20, true); g.Move(0, 20, 20, true);
Check(!g.End(0, 20, 20, true), "drag returning to its origin is not a tap");
g.Begin(0, 20, 20, true); g.Move(0, 20, 20, false);
Check(!g.End(0, 20, 20, true), "leaving and returning to hitbox cancels tap");
g.Begin(0, 20, 20, true);
Check(g.ShowHint() && !g.ShowHint(), "long press shows one hint");
Check(!g.End(0, 20, 20, true), "long press release does not open a pile");
g.Begin(0, 20, 20, true); g.Begin(1, 20, 20, true);
Check(!g.End(1, 20, 20, true) && g.IsPressed, "second finger release does not finish first gesture");
Check(!g.End(0, 20, 20, true), "two-finger gesture cancels pile tap");
g.Begin(0, 20, 20, true); g.Reset();
Check(!g.End(0, 20, 20, true), "focus loss cancels pending touch");
g.Begin(-2, 20, 20, false);
Check(!g.ShowHint() && g.End(-2, 20, 20, true), "physical mouse keeps single-click behavior");
g.Begin(4, 20, 20, true);
Check(!g.End(7, 20, 20, true) && g.End(4, 20, 20, true), "unrelated pointer release cannot steal a tap");
Console.WriteLine($"{count} gesture checks passed.");
