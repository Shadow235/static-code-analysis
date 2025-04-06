using StaticCodeAnalysis.Target.B;
using StaticCodeAnalysis.Target.C;

namespace StaticCodeAnalysis.Target.A;

public class Level1
{
    public string Method1(string a)
    {
        var level2 = new Level2();
        level2.Method2(a, "b");
        
        return a;
    }

    public string Method2(string a, string b)
    {
        return a + b;
    }

    public string Method3(string a, string b, string c)
    {
        var level2 = new Level2();
        level2.Method1(a);
        return a + b + c;
    }
}