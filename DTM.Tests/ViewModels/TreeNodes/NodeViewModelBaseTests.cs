using DTM.ViewModels.TreeNodes;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ViewModels.TreeNodes;

public class NodeViewModelBaseTests
{
    private sealed class ConcreteNode : NodeViewModelBase
    {
        public int ExpandCount;
        protected override void OnExpanded() => ExpandCount++;
    }

    [Fact]
    public void IsExpanded_SetTrue_CallsOnExpanded()
    {
        var node = new ConcreteNode();
        node.IsExpanded = true;
        node.ExpandCount.Should().Be(1);
    }

    [Fact]
    public void IsExpanded_SetFalse_DoesNotCallOnExpanded()
    {
        var node = new ConcreteNode();
        node.IsExpanded = false;
        node.ExpandCount.Should().Be(0);
    }

    [Fact]
    public void IsExpanded_SetTrueTwice_CalledTwice()
    {
        var node = new ConcreteNode();
        node.IsExpanded = true;
        node.IsExpanded = false;
        node.IsExpanded = true;
        node.ExpandCount.Should().Be(2);
    }

    [Fact]
    public void Header_Default_IsEmpty()
    {
        new ConcreteNode().Header.Should().BeEmpty();
    }

    [Fact]
    public void Children_Default_IsEmpty()
    {
        new ConcreteNode().Children.Should().BeEmpty();
    }
}
