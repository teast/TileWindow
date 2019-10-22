using Moq;
using TileWindow.Nodes;
using TileWindow.Trackers;
using Xunit;
using TileWindow.Tests.TestHelpers;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TileWindow.Tests.Trackers
{
    public class FocusTrackerTests
    {
        Random rnd = new Random();

        [Fact]
        public void When_TrackingANode_Then_ListenToNodesEvents()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();

            // Act
            var result = sut.Track(node.Object);

            // Assert
            node.Object.EventShouldNotBeNull(nameof(Node.Deleted));
            node.Object.EventShouldNotBeNull(nameof(Node.WantFocus));
        }

        [Fact]
        public void When_UntrackingANode_Then_StopListenToNodesEvents()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.Track(node.Object);
            node.Object.EventShouldNotBeNull(nameof(Node.Deleted));
            node.Object.EventShouldNotBeNull(nameof(Node.WantFocus));

            // Act
            var result = sut.Untrack(node.Object);

            // Assert
            node.Object.EventShouldBeNull(nameof(Node.Deleted));
            node.Object.EventShouldBeNull(nameof(Node.WantFocus));
        }

        [Fact]
        public void When_UntrackingANode_And_NodeIsFocusNode_Then_RemoveFocusNode()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.Track(node.Object);
            node.Object.SetFocus();
            sut.FocusNode().Should().BeEquivalentTo(node.Object);

            // Act
            var result = sut.Untrack(node.Object);

            // Assert
            sut.FocusNode().Should().BeNull();
        }

        [Fact]
        public void When_UntrackingANode_And_NodeIsSomeonesFocusNode_Then_RemoveItFocusNode()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var node2 = new Mock<Node>(new RECT(), Direction.Vertical, null) { CallBase = true };
            var sut = CreateSut();
            sut.Track(node.Object);
            sut.Track(node2.Object);
            node.Object.Parent = node2.Object;
            node.Object.SetFocus();
            sut.MyLastFocusNode(node2.Object).Should().BeEquivalentTo(node.Object);

            // Act
            var result = sut.Untrack(node.Object);

            // Assert
            sut.MyLastFocusNode(node2.Object).Should().BeNull();
        }

        [Fact]
        public void When_WantFocus_Then_UpdateFocusNode()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.FocusNode().Should().BeNull();
            sut.Track(node.Object);

            // Act
            node.Object.SetFocus();
            var result = sut.FocusNode();

            // Assert
            result.Should().BeEquivalentTo(node.Object);
        }

        [Fact]
        public void When_WantFocus_And_NodeIsAlreadyFocusNode_Then_DoNothing()
        {
            // Arrange
            var focusChanged = false;
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.Track(node.Object);
            node.Object.SetFocus();
            sut.FocusChanged += (sender, arg) => focusChanged = true;

            // Act
            node.Object.SetFocus();

            // Assert
            sut.FocusNode().Should().BeEquivalentTo(node.Object);
            focusChanged.Should().BeFalse();
        }

        [Fact]
        public void When_TraceToFocusNode_And_ItIsChildOfNode_Then_ReturnTrace()
        {
            // Arrange
            var sut = CreateSut();
            var nodes = CreateChain(4, sut);
            var focusNode = nodes.Last();
            foreach(var n in nodes)
                sut.Track(n);

            focusNode.SetFocus();

            // Act
            var result = sut.TraceToFocusNode(nodes.Skip(1).First());

            // Assert
            result.Should().NotContain(nodes.First());
            foreach(var node in nodes.Skip(1))
                result.Should().Contain(node);
            result.Last().Should().BeEquivalentTo(focusNode);
        }

        [Fact]
        public void When_TraceToFocusNode_And_ItIsNotChildOfNode_Then_ReturnNull()
        {
            // Arrange
            var sut = CreateSut();
            var nodes = CreateChain(4, sut);
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };

            foreach(var n in nodes)
                sut.Track(n);
            sut.Track(focusNode.Object);
            focusNode.Object.SetFocus();

            // Act
            var result = sut.TraceToFocusNode(nodes.First());

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void When_NoFocusHappen_Then_ReturnNullForLastChildFocus()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.Track(node.Object);

            // Act
            var result = sut.MyLastFocusNode(node.Object);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void When_FocusHappen_Then_ReturnChildThatWaslastFocused()
        {
            // Arrange
            var sut = CreateSut();
            var nodes = CreateChain(4, sut);
            var focusNode = nodes.Last();
            var first = nodes.First();
            var second = nodes.Skip(1).First();

            foreach(var n in nodes)
                sut.Track(n);
            focusNode.SetFocus();

            // Act
            var result = sut.MyLastFocusNode(first);

            // Assert
            result.Should().BeEquivalentTo(second);
        }

        [Fact]
        public void When_FocusHappenAnSecondTime_Then_ReturnChildThatWasLastFocused()
        {
            // Arrange
            var sut = CreateSut();
            var nodes = CreateChain(4, sut);
            var firstFocusNode = nodes.Last();
            var first = nodes.First();
            var second = nodes.Skip(1).First();
            var secondFocusNode = new Mock<Node>(GetRect(), Direction.Horizontal, null) { CallBase = true };
            foreach(var n in nodes)
                sut.Track(n);
            sut.Track(secondFocusNode.Object);
            firstFocusNode.SetFocus();
            secondFocusNode.Object.Parent = first;
            secondFocusNode.Object.SetFocus();

            // Act
            var result = sut.MyLastFocusNode(first);

            // Assert
            result.Should().NotBeEquivalentTo(second);
            result.Should().BeEquivalentTo(secondFocusNode.Object);
        }

        #region Helpers
        private RECT GetRect()
        {
                var x = rnd.Next(1, 200);
                var y = rnd.Next(1, 200);
                var x2 = x + rnd.Next(1, 200);
                var y2 = y + rnd.Next(1, 200);
                return new RECT(x, y, x2, y2);
        }

        private Node[] CreateChain(int depth, IFocusTracker tracker)
        {
            var nodes = new List<Node>();
            var desktop = new Mock<IVirtualDesktop>();
            desktop.SetupGet(m => m.FocusTracker).Returns(tracker);
            var mockFirst = NodeHelper.CreateMockContainer(GetRect());
            var first = mockFirst.Object;
            mockFirst.SetupGet(m => m.Desktop).Returns(desktop.Object);
            nodes.Add(first);
            Node prev = first;
            for(int i = 1; i < depth; i++)
            {
                var n = NodeHelper.CreateMockContainer(GetRect()).Object;
                prev.AddNodes(n);
                prev = n;
                nodes.Add(n);
            }

            var node = new Mock<Node>(GetRect(), Direction.Horizontal, null) { CallBase = true };
            prev.AddNodes(node.Object);
            nodes.Add(node.Object);
            return nodes.ToArray();
            /*
            var first = NodeHelper.CreateMockContainer();
            var prev = first;
            for(int i = 0; i < depth; i++)
            {
                var next = NodeHelper.CreateMockContainer();
                prev.Object.AddNodes(next.Object);
                prev = next;
            }

            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            prev.Object.AddNodes(node.Object);

            return (first, node);
            */
        }

        public FocusTracker CreateSut()
        {
            return new FocusTracker();
        }
        #endregion
    }
}