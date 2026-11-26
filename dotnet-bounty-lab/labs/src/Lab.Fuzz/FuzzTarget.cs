namespace Lab.Fuzz;

/// <summary>Thrown by a target when an oracle (round-trip, differential, safety invariant) is violated.</summary>
public sealed class InvariantViolation(string message) : Exception(message);

public sealed record FuzzTarget(
    string Name,
    string Description,
    string SourcePath,                 // where the code under test lives inside the VMR (src/vmr/<ver>/...)
    string[] InstrumentAssemblies,     // for SharpFuzz: which shipped assemblies hold the parser
    Type[] ExpectedExceptions,         // documented rejection exceptions - anything else is a finding
    byte[][] Seeds,
    string[] Dictionary,
    Action<byte[]> Execute,
    int SlowMs = 1500)                 // single input slower than this = algorithmic-complexity candidate
{
    public bool IsExpected(Exception ex)
    {
        if (ex is InvariantViolation) return false;
        var t = ex.GetType();
        return ExpectedExceptions.Any(e => e.IsAssignableFrom(t));
    }
}
