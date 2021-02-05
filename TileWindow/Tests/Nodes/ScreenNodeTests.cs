using FluentAssertions;
using Moq;
using TileWindow.Nodes;
using TileWindow.Nodes.Creaters;
using TileWindow.Trackers;
using TileWindow.Tests.TestHelpers;
using Xunit;
using TileWindow.Nodes.Renderers;
using System.Collections.Generic;

namespace TileWindow.Tests.Nodes
{
    public class ScreenNodeTests
    {
#region SetFocus
        [Fact]
        public void When_SetFocus_And_ThereIsAnFullscreenNode_Then_SetFocusToFullscreenNode()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var dir = TransferDirection.Left;
            var sut = CreateSut();
            sut.AddNodes(node.Object);
            node.Object.Style = NodeStyle.FullscreenOne;
            
            // Act
            sut.SetFocus(dir);

            // Assert
            node.Verify(m => m.SetFocus(dir));
        }

        // Bug: #95, Never triggered parents FocusNode when no childs
        [Fact]
        public void When_SetFocus_And_NoChilds_Then_SignalEventWantFocus()
        {
            // Arrange
            var dir = TransferDirection.Left;
            var wantFocusCalled = false;
            var sut = CreateSut();
            sut.WantFocus += (sender, arg) => wantFocusCalled = true;

            // Act
            sut.SetFocus(dir);

            // Assert
            wantFocusCalled.Should().BeTrue();
        }
#endregion
#region ChildNodeStyleChange
        [Fact]
        public void When_ChildStyleChangeToFullscreen_Then_SetChildToFullscreenNodeAndChildsRectShouldBeUpdated()
        {
            // Arrange
            var expected = new RECT(66, 66, 666, 666);
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut(rect: expected);
            sut.AddNodes(node.Object);
            
            // Act
            node.Object.Style = NodeStyle.FullscreenOne;

            // Assert
            sut.FullscreenNode.Should().BeEquivalentTo(node.Object);
            node.Verify(m => m.SetFullscreenRect(expected));
        }
        [Fact]
        public void When_ChildStyleChangeToFullscreen_And_ThereIsAnFullscreenNodeAlready_Then_ChangeOldFullscreenNodeToTile()
        {
            // Arrange
            var oldNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.AddNodes(node.Object);
            sut.AddNodes(oldNode.Object);
            oldNode.Object.Style = NodeStyle.FullscreenOne;
            sut.FullscreenNode.Should().BeEquivalentTo(oldNode.Object);

            // Act
            node.Object.Style = NodeStyle.FullscreenOne;

            // Assert
            sut.FullscreenNode.Should().BeEquivalentTo(node.Object);
            oldNode.Object.Style.Should().Be(NodeStyle.Tile);
        }
        [Fact]
        public void When_ChildStyleChangeFromFullscreenToTile_Then_RemoveItFromFullscreenNode_And_UpdatesChildsRectToTileRect()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.AddNodes(node.Object);
            node.Object.Style = NodeStyle.FullscreenOne;
            sut.FullscreenNode.Should().BeEquivalentTo(node.Object);
            
            // Act
            node.Object.Style = NodeStyle.Tile;

            // Assert
            sut.FullscreenNode.Should().BeNull();
            node.Verify(m => m.UpdateRect(It.IsAny<RECT>()));
        }
#endregion
#region TransferNode
        [Fact]
        public void When_TransferNode_And_AnotherNodeIsFullscreen_Then_MakeSureFullscreenNodeGotFocus()
        {
            // Arrange
            var node = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut(direction: Direction.Horizontal);
            sut.AddNodes(node.Object);
            node.Object.Style = NodeStyle.FullscreenOne;
            
            // Act
            sut.TransferNode(null, CreateSut(), TransferDirection.Left, true);

            // Assert
            node.Verify(m => m.SetFocus(It.IsAny<TransferDirection>()));
        }
#endregion
#region TransferAllChilds
        [Fact]
        public void When_TransferAllChilds_Then_TransferThemAll()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var child1 = NodeHelper.CreateMockNode(direction: Direction.Horizontal);
            var child2 = NodeHelper.CreateMockNode(direction: Direction.Vertical);
            var sut = CreateSut();
            var destination = CreateSut();
            sut.AddNodes(child1.Object, child2.Object);

            // Act
            sut.TransferAllChilds(destination, direction);

            // Assert
            sut.Childs.Should().BeEmpty();
            destination.Childs.Should().Contain(child1.Object);
            destination.Childs.Should().Contain(child2.Object);
        }

        [Fact]
        public void When_TransferAllChilds_Then_DisconnectChild()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var child = NodeHelper.CreateMockNode(direction: Direction.Horizontal);
            var sut = CreateSut();
            var destination = NodeHelper.CreateMockScreen();
            sut.AddNodes(child.Object);
            child.Invocations.Clear();
            destination.Setup(m => m.TransferNode(null, child.Object, direction, It.IsAny<bool>())).Returns(true);
            
            // Act
            sut.TransferAllChilds(destination.Object, direction);

            // Assert
            child.Object.TestDisconnect(sut);
        }

        [Fact]
        public void When_TransferAllChilds_And_ChildIsFullscreen_Then_ChangeItToTile()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var child = NodeHelper.CreateMockNode(direction: Direction.Horizontal);
            var sut = CreateSut();
            var destination = NodeHelper.CreateMockScreen();
            child.SetupGet(m => m.Style).Returns(NodeStyle.FullscreenOne);
            sut.AddNodes(child.Object);
            destination.Setup(m => m.TransferNode(null, child.Object, direction, It.IsAny<bool>())).Returns(true);
            
            // Act
            sut.TransferAllChilds(destination.Object, direction);

            // Assert
            child.VerifySet(m => m.Style = NodeStyle.Tile);
        }
#endregion
#region helpers
        private ScreenNode CreateSut(RECT? rect = null, Direction direction = Direction.Horizontal)
        {
            return CreateSut(out _, out _, out _, out _, out _, rect, direction);
        }

        private ScreenNode CreateSut(out Mock<Node> parent, out Mock<IVirtualDesktop> virtualDesktop, out Mock<FocusTracker> focusTracker, out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Direction direction = Direction.Horizontal)
        {
            var renderer = new Mock<IRenderer>();
            parent = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            virtualDesktop = new Mock<IVirtualDesktop>();
            focusTracker = new Mock<FocusTracker>() { CallBase = true };
            containerCreater = new Mock<IContainerNodeCreater>();
            windowTracker = new Mock<IWindowTracker>();

            var screen = new ScreenNode(
                "screen",
                renderer.Object,
                containerCreater.Object,
                windowTracker.Object,
                rect ?? new RECT(10, 20, 30, 40),
                direction);

            renderer.Setup(m => m.Update(It.IsAny<List<int>>())).Returns(() => (true, screen.Rect));
            screen.Parent = parent.Object;
            virtualDesktop.SetupGet(m => m.FocusTracker).Returns(focusTracker.Object);
            parent.SetupGet(m => m.Desktop).Returns(virtualDesktop.Object);

            return screen;
        }
#endregion
    }
}