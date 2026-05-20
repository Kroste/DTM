using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using DTM.Data.Terminal;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Terminal;

public class DtmPSHostTests
{
    // ------------------------------------------------------------------ DtmPSHost

    [Fact]
    public void Name_IsDtmPowerShellHost()
    {
        var ui = new DtmPSHostUI();
        new DtmPSHost(ui).Name.Should().Be("DTM-PowerShellHost");
    }

    [Fact]
    public void Version_Is1_0()
    {
        var ui = new DtmPSHostUI();
        new DtmPSHost(ui).Version.Should().Be(new Version(1, 0));
    }

    [Fact]
    public void InstanceId_IsNotEmptyGuid()
    {
        var ui = new DtmPSHostUI();
        new DtmPSHost(ui).InstanceId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UI_ReturnsSameInstance()
    {
        var ui = new DtmPSHostUI();
        var host = new DtmPSHost(ui);
        host.UI.Should().BeSameAs(ui);
    }

    [Fact]
    public void VoidMethods_DoNotThrow()
    {
        var host = new DtmPSHost(new DtmPSHostUI());
        Action act = () =>
        {
            host.EnterNestedPrompt();
            host.ExitNestedPrompt();
            host.NotifyBeginApplication();
            host.NotifyEndApplication();
            host.SetShouldExit(0);
        };
        act.Should().NotThrow();
    }

    // ------------------------------------------------------------------ DtmPSHostUI — Output

    [Fact]
    public void Write_CallsOnOutput()
    {
        string? received = null;
        var ui = new DtmPSHostUI { OnOutput = s => received = s };
        ui.Write("hello");
        received.Should().Be("hello");
    }

    [Fact]
    public void Write_WithColors_CallsOnOutput()
    {
        string? received = null;
        var ui = new DtmPSHostUI { OnOutput = s => received = s };
        ui.Write(ConsoleColor.Red, ConsoleColor.Black, "colored");
        received.Should().Be("colored");
    }

    [Fact]
    public void WriteLine_AddsTrailingNewline()
    {
        string? received = null;
        var ui = new DtmPSHostUI { OnOutput = s => received = s };
        ui.WriteLine("line");
        received.Should().Be("line\n");
    }

    [Fact]
    public void WriteErrorLine_CallsOnError_WithNewline()
    {
        string? received = null;
        var ui = new DtmPSHostUI { OnError = s => received = s };
        ui.WriteErrorLine("err");
        received.Should().Be("err\n");
    }

    [Fact]
    public void WriteDebugLine_PrefixesDebug()
    {
        string? received = null;
        var ui = new DtmPSHostUI { OnOutput = s => received = s };
        ui.WriteDebugLine("msg");
        received.Should().Be("DEBUG: msg\n");
    }

    [Fact]
    public void WriteWarningLine_PrefixesWarning()
    {
        string? received = null;
        var ui = new DtmPSHostUI { OnOutput = s => received = s };
        ui.WriteWarningLine("warn");
        received.Should().Be("WARNING: warn\n");
    }

    [Fact]
    public void Write_NullCallback_DoesNotThrow()
    {
        var ui = new DtmPSHostUI();
        Action act = () => { ui.Write("x"); ui.WriteErrorLine("e"); };
        act.Should().NotThrow();
    }

    // ------------------------------------------------------------------ DtmPSHostUI — Queue

    [Fact]
    public void ProvideInput_ThenReadLine_ReturnsLine()
    {
        var ui = new DtmPSHostUI();
        ui.ProvideInput("hello");
        ui.ReadLine().Should().Be("hello");
    }

    [Fact]
    public void ProvideInput_MultipleLines_FifoOrder()
    {
        var ui = new DtmPSHostUI();
        ui.ProvideInput("first");
        ui.ProvideInput("second");
        ui.ProvideInput("third");
        ui.ReadLine().Should().Be("first");
        ui.ReadLine().Should().Be("second");
        ui.ReadLine().Should().Be("third");
    }

    [Fact]
    public void Cancel_ThenReadLine_ReturnsEmpty()
    {
        var ui = new DtmPSHostUI();
        ui.Cancel();
        ui.ReadLine().Should().Be(string.Empty);
    }

    [Fact]
    public void ReadLineAsSecureString_ContainsCorrectChars()
    {
        var ui = new DtmPSHostUI();
        ui.ProvideInput("abc");
        var ss = ui.ReadLineAsSecureString();
        ss.Length.Should().Be(3);
    }

    // ------------------------------------------------------------------ DtmPSHostUI — Prompts

    [Fact]
    public void PromptForChoice_EmptyInput_ReturnsDefault()
    {
        var ui = new DtmPSHostUI();
        var choices = new Collection<ChoiceDescription> { new("&Yes"), new("&No") };
        ui.ProvideInput("");
        ui.PromptForChoice("caption", "message", choices, 0).Should().Be(0);
    }

    [Fact]
    public void PromptForChoice_NumericInput_ReturnsIndex()
    {
        var ui = new DtmPSHostUI();
        var choices = new Collection<ChoiceDescription> { new("&Yes"), new("&No") };
        ui.ProvideInput("1");
        ui.PromptForChoice("caption", "message", choices, 0).Should().Be(1);
    }

    // ------------------------------------------------------------------ DtmPSHostUI — Credential

    [Fact]
    public void PromptForCredential_CallsOnError_WithHint()
    {
        string? error = null;
        var ui = new DtmPSHostUI { OnError = s => error = s };
        ui.PromptForCredential("cap", "msg", "user", "target");
        error.Should().Contain("credential.xml");
    }
}
