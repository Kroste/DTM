using DTM.Data.Terminal;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Terminal;

[Collection("serial")]
public class TerminalBusTests : IDisposable
{
    private FakeSession? _registered;

    private FakeSession Register(bool running = true)
    {
        var s = new FakeSession { IsRunning = running };
        TerminalBus.RegisterPowerShellSession(s);
        _registered = s;
        return s;
    }

    public void Dispose()
    {
        if (_registered is not null)
            TerminalBus.UnregisterPowerShellSession(_registered);
        _registered = null;
    }

    // ------------------------------------------------------------------ HasPowerShellSession

    [Fact]
    public void HasPowerShellSession_NoSession_ReturnsFalse()
    {
        TerminalBus.HasPowerShellSession.Should().BeFalse();
    }

    [Fact]
    public void HasPowerShellSession_RegisteredRunning_ReturnsTrue()
    {
        Register(running: true);
        TerminalBus.HasPowerShellSession.Should().BeTrue();
    }

    [Fact]
    public void HasPowerShellSession_RegisteredNotRunning_ReturnsFalse()
    {
        Register(running: false);
        TerminalBus.HasPowerShellSession.Should().BeFalse();
    }

    // ------------------------------------------------------------------ Register / Unregister

    [Fact]
    public void Unregister_RemovesSession()
    {
        var s = Register();
        TerminalBus.UnregisterPowerShellSession(s);
        _registered = null;
        TerminalBus.HasPowerShellSession.Should().BeFalse();
    }

    [Fact]
    public void Unregister_DifferentSession_DoesNotRemove()
    {
        Register();
        var other = new FakeSession { IsRunning = true };
        TerminalBus.UnregisterPowerShellSession(other); // different object → should not remove
        TerminalBus.HasPowerShellSession.Should().BeTrue();
    }

    // ------------------------------------------------------------------ RunFocSqlAction

    [Fact]
    public void RunFocSqlAction_NoSession_CallsOnUnavailable()
    {
        bool called = false;
        TerminalBus.RunFocSqlAction("Backup-Database", "MyDB", null, "test",
            onUnavailable: () => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void RunFocSqlAction_NoSession_NullCallback_NoThrow()
    {
        Action act = () => TerminalBus.RunFocSqlAction("Backup-Database", "MyDB", null, "test");
        act.Should().NotThrow();
    }

    [Fact]
    public void RunFocSqlAction_WithSession_SendsCommand()
    {
        var s = Register();
        TerminalBus.RunFocSqlAction("Backup-Database", "MyDB", null, "test");
        s.SentCommands.Should().HaveCount(1);
        s.SentCommands[0].Should().Contain("Backup-Database");
        s.SentCommands[0].Should().Contain("MyDB");
    }

    // ------------------------------------------------------------------ RunFocSqlSimple

    [Fact]
    public void RunFocSqlSimple_NoSession_CallsOnUnavailable()
    {
        bool called = false;
        TerminalBus.RunFocSqlSimple("Restore-Snapshot", "MyDB", "", "test",
            onUnavailable: () => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void RunFocSqlSimple_WithSession_CommandContainsFunctionAndDb()
    {
        var s = Register();
        TerminalBus.RunFocSqlSimple("Restore-Snapshot", "MyDB", "", "test");
        s.SentCommands.Should().HaveCount(1);
        s.SentCommands[0].Should().Contain("Restore-Snapshot");
        s.SentCommands[0].Should().Contain("'MyDB'");
    }

    [Fact]
    public void RunFocSqlSimple_WithExtraArgs_AppendedToCommand()
    {
        var s = Register();
        TerminalBus.RunFocSqlSimple("Set-Archive-Log", "MyDB", "-Off", "test");
        s.SentCommands[0].Should().EndWith("-Off");
    }

    [Fact]
    public void RunFocSqlSimple_DbSingleQuote_Escaped()
    {
        var s = Register();
        TerminalBus.RunFocSqlSimple("Restore-Snapshot", "DB's-Name", "", "test");
        s.SentCommands[0].Should().Contain("DB''s-Name");
    }

    // ------------------------------------------------------------------ SendScript

    [Fact]
    public void SendScript_Empty_NoSend()
    {
        var s = Register();
        TerminalBus.SendScript(string.Empty);
        s.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public void SendScript_Whitespace_NoSend()
    {
        var s = Register();
        TerminalBus.SendScript("   ");
        s.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public void SendScript_WithSession_SendsScript()
    {
        var s = Register();
        TerminalBus.SendScript("Write-Host 'hello'");
        s.SentCommands.Should().HaveCount(1);
        s.SentCommands[0].Should().Be("Write-Host 'hello'");
    }

    [Fact]
    public void SendScript_NoSession_NoThrow()
    {
        Action act = () => TerminalBus.SendScript("Write-Host 'hi'");
        act.Should().NotThrow();
    }

    // ------------------------------------------------------------------ FakeSession

    private sealed class FakeSession : ITerminalSession
    {
        public bool IsRunning { get; set; } = true;
        public List<string> SentCommands { get; } = new();

        public Task SendCommandAsync(string command, CancellationToken _ = default)
        {
            SentCommands.Add(command);
            return Task.CompletedTask;
        }

        public void Dispose() { }
        public Task StartAsync(CancellationToken _ = default) => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;

#pragma warning disable CS0067
        public event EventHandler<string>? OutputReceived;
        public event EventHandler<string>? ErrorReceived;
        public event EventHandler<string>? Notice;
        public event EventHandler? SessionEnded;
#pragma warning restore CS0067
    }
}
