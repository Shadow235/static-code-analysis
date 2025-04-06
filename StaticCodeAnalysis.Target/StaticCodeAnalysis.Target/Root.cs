using StaticCodeAnalysis.Target.A;
using StaticCodeAnalysis.Target.B;
using StaticCodeAnalysis.Target.C;

namespace StaticCodeAnalysis.Target;

public class Root
{
    public void Execute()
    {
        new Level1().Method1("a");
        new Level1().Method3("a", "b", "c");
    }
}