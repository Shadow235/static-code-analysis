using StaticCodeAnalysis.Target.C;

namespace StaticCodeAnalysis.Target.B;

public class Level2
{
    public bool Method1(string a)
    {
        var level3 = new Level3();
        level3.Method3(1, 2, 3);
        
        return true;
    }

    public bool Method2(string a, string b)
    {
        var level3 = new Level3();
        level3.Method3(1, 2, 3);
        
        return false;
    }

    public bool Method3(string a, string b, string c)
    {
        return true;
    }
}