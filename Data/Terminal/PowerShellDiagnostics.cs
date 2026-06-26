using System.Management.Automation;
using System.Text;

namespace DTM.Data.Terminal;

/// <summary>
/// Helfer fuer In-Process-PowerShell-Aufrufe — formatiert die Fehler-,
/// Warnung- und Info-Streams zu einer aussagekraeftigen Diagnose-Nachricht,
/// damit ein <see cref="InvalidOperationException"/> nicht nur „… fehlgeschlagen"
/// sagt, sondern auch den ScriptName/Linenumber/FullyQualifiedErrorId der
/// urspruenglichen PowerShell-Fehler enthaelt.
/// </summary>
internal static class PowerShellDiagnostics
{
    public static void ThrowIfErrors(PowerShell ps, string stage)
    {
        if (!ps.HadErrors) return;

        StringBuilder sb = new();
        sb.Append(stage).Append(" fehlgeschlagen");

        if (ps.Streams.Error.Count > 0)
        {
            sb.Append(':');
            foreach (ErrorRecord err in ps.Streams.Error)
                AppendErrorRecord(sb, err);
        }
        else
        {
            sb.Append(" (HadErrors=true, aber Streams.Error leer — siehe Warnungen unten)");
        }

        if (ps.Streams.Warning.Count > 0)
        {
            sb.Append("\nWarnungen:");
            foreach (WarningRecord w in ps.Streams.Warning)
                sb.Append("\n  - ").Append(w.Message);
        }

        // Information-Stream (Write-Host / Write-Information) kann zusaetzlich
        // Kontext liefern, der ohne Fehler-Position waere — z.B. „Baue
        // Verbindung zum Server X auf" direkt vor dem Crash.
        if (ps.Streams.Information.Count > 0)
        {
            int lastN = Math.Min(ps.Streams.Information.Count, 5);
            sb.Append("\nLetzte Information-Stream-Eintraege (max ").Append(lastN).Append("):");
            for (int i = ps.Streams.Information.Count - lastN; i < ps.Streams.Information.Count; i++)
                sb.Append("\n  - ").Append(ps.Streams.Information[i].MessageData?.ToString() ?? "");
        }

        throw new InvalidOperationException(sb.ToString());
    }

    private static void AppendErrorRecord(StringBuilder sb, ErrorRecord err)
    {
        sb.Append("\n  - ");
        string msg = err.Exception?.Message ?? err.ToString();
        sb.Append(msg);

        if (!string.IsNullOrEmpty(err.FullyQualifiedErrorId))
            sb.Append(" [").Append(err.FullyQualifiedErrorId).Append(']');

        InvocationInfo? inv = err.InvocationInfo;
        if (inv is not null)
        {
            if (!string.IsNullOrEmpty(inv.ScriptName))
                sb.Append(" @ ").Append(inv.ScriptName).Append(':').Append(inv.ScriptLineNumber);
            else if (inv.ScriptLineNumber > 0)
                sb.Append(" @ Zeile ").Append(inv.ScriptLineNumber);
        }

        if (err.Exception?.InnerException is { } inner)
            sb.Append("\n      Ursache: ").Append(inner.Message);
    }
}
