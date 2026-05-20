using DTM.ViewModels;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ViewModels;

public class SessionsViewModelTests
{
    [Fact]
    public void Initial_SessionsIsEmpty()
    {
        new SessionsViewModel().Sessions.Should().BeEmpty();
    }

    [Fact]
    public void SetSessions_Null_ClearsCollection()
    {
        var vm = new SessionsViewModel();
        vm.SetSessions([new Session { Username = "u" }]);
        vm.SetSessions(null);
        vm.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void SetSessions_Empty_ClearsCollection()
    {
        var vm = new SessionsViewModel();
        vm.SetSessions([new Session { Username = "u" }]);
        vm.SetSessions([]);
        vm.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void SetSessions_Items_AddsAll()
    {
        var vm = new SessionsViewModel();
        vm.SetSessions([
            new Session { Username = "alice" },
            new Session { Username = "bob" }
        ]);
        vm.Sessions.Should().HaveCount(2);
        vm.Sessions[0].Username.Should().Be("alice");
        vm.Sessions[1].Username.Should().Be("bob");
    }

    [Fact]
    public void SetSessions_SecondCall_ReplacesPrevious()
    {
        var vm = new SessionsViewModel();
        vm.SetSessions([new Session { Username = "old" }]);
        vm.SetSessions([new Session { Username = "new1" }, new Session { Username = "new2" }]);
        vm.Sessions.Should().HaveCount(2);
        vm.Sessions.Should().NotContain(s => s.Username == "old");
    }
}
