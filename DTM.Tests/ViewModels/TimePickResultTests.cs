using DTM.ViewModels;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ViewModels;

public class TimePickResultTests
{
    [Fact]
    public void Cancel_IsCancelled()
    {
        var r = TimePickResult.Cancel();
        r.Cancelled.Should().BeTrue();
        r.When.Should().BeNull();
    }

    [Fact]
    public void Immediate_NotCancelled_WhenIsNull()
    {
        var r = TimePickResult.Immediate();
        r.Cancelled.Should().BeFalse();
        r.When.Should().BeNull();
    }

    [Fact]
    public void At_NotCancelled_WhenSet()
    {
        var r = TimePickResult.At(DateTime.Today);
        r.Cancelled.Should().BeFalse();
        r.When.Should().NotBeNull();
    }

    [Fact]
    public void At_WhenValue_MatchesInput()
    {
        var dt = new DateTime(2026, 6, 15, 10, 30, 0);
        TimePickResult.At(dt).When.Should().Be(dt);
    }
}
