using FluentAssertions;
using Moq;
using TileWindow.Nodes;
using TileWindow.Nodes.Creaters;
using TileWindow.Trackers;
using TileWindow.Tests.TestHelpers;
using Xunit;

namespace TileWindow.Tests.Handlers
{
    public class VirtualDesktopTests
    {

#region PostInit
        [Fact]
        public void When__CreatingNewVirtualDesktop_Then_SetFocusToFirstChild_So_FocusNodeIsNotNull()
        {
            // Arrange
            var screen = NodeHelper.CreateMockScreen(callPostInit: false);
            var sut = CreateSut(callPostInit: false);

            // Act
            sut.PostInit(screen.Object);

            // Assert
            screen.Verify(m => m.SetFocus(null));
        }
#endregion
#region AddFloatingNode
        [Fact]
        public void When_AddFloatingNode_Then_NodeShouldBeTakenChangedToFloating_And_AddedToFloatingNodes()
        {
            // Arrange
            var child = NodeHelper.CreateMockScreen(direction: Direction.Vertical, callPostInit: false).Object;
            var screen = NodeHelper.CreateMockScreen(callPostInit: false).Object;
            var sut = CreateSut(callPostInit: false);
            sut.PostInit(screen);

            // Act
            var result = sut.AddFloatingNode(child);

            // Assert
            result.Should().BeTrue();
            child.TestTakeOver(sut);
            sut.FloatingNodes.Should().Contain(child);
        }

        [Fact]
        public void When_AddFloatingNode_And_DesktopIsNotVisible_Then_NodeShouldBeMadeHidden()
        {
            // Arrange
            var child = NodeHelper.CreateMockScreen(direction: Direction.Vertical, callPostInit: false);
            var screen = NodeHelper.CreateMockScreen(callPostInit: false).Object;
            var sut = CreateSut(callPostInit: false);
            sut.PostInit(screen);

            // Act
            sut.Hide();
            var result = sut.AddFloatingNode(child.Object);

            // Assert
            child.Verify(m => m.Hide());
        }

        [Fact]
        public void When_AddFloatingNode_And_DesktopIsVisible_Then_NodeShouldBeMadeVisible()
        {
            // Arrange
            var child = NodeHelper.CreateMockScreen(direction: Direction.Vertical, callPostInit: false);
            var screen = NodeHelper.CreateMockScreen(callPostInit: false).Object;
            var sut = CreateSut(callPostInit: false);
            sut.PostInit(screen);

            // Act
            var result = sut.AddFloatingNode(child.Object);

            // Assert
            child.Verify(m => m.Show());
        }
#endregion
#region HandleSwitchFloating
        [Fact]
        public void When_HandleSwitchFloating_And_FocusNodeIsVirtualDesktopChild_Then_DoNothing()
        {
            // Arrange
            var child = NodeHelper.CreateMockScreen();
            var sut = CreateMockSut(out _, out _, new RECT(10, 10, 30, 40), Direction.Horizontal, new ScreenNode[] { child.Object });

            // Act
            sut.Object.HandleSwitchFloating();

            // Assert
            child.Object.Parent.Should().BeEquivalentTo(sut.Object);
            sut.Verify(m => m.DisconnectChild(child.Object), Times.Never());
        }

        [Fact]
        public void When_HandleSwitchFloating_And_FocusNodeIsNotFloating_Then_ChangeItToFloating()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var screen = NodeHelper.CreateMockScreen(direction: Direction.Vertical, callPostInit: false);
            var sut = CreateSut(out Mock<FocusTracker> focusTracker, childs: new ScreenNode[] { screen.Object });
            screen.Object.PostInit(child.Object);
            focusTracker.Setup(m => m.FocusNode()).Returns(child.Object);
            child.Object.Style = NodeStyle.Tile;

            // Act
            sut.HandleSwitchFloating();

            // Assert
            screen.Verify(m => m.DisconnectChild(child.Object));
            child.VerifySet(m => m.Parent = sut);
            child.VerifySet(m => m.Style = NodeStyle.Floating);
            sut.FloatingNodes.Should().Contain(child.Object);

        }
        [Fact]
        public void When_HandleSwitchFloating_And_ConvertingToFloatingNode_And_DisconnectChildReturnFalse_Then_Abort()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var screen = NodeHelper.CreateMockScreen(direction: Direction.Vertical, callPostInit: false);
            var sut = CreateSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: new ScreenNode[] { screen.Object });
            screen.Object.PostInit(child.Object);
            focusTracker.Setup(m => m.FocusNode()).Returns(child.Object);
            child.Object.Style = NodeStyle.Tile;
            screen.Setup(m => m.DisconnectChild(child.Object)).Returns(false).Verifiable();

            // Act
            sut.HandleSwitchFloating();

            // Assert
            screen.VerifyAll();
            child.Object.Parent.Should().NotBeEquivalentTo(sut);
            child.Object.Style.Should().NotBe(NodeStyle.Floating);
            sut.FloatingNodes.Should().NotContain(child.Object);
        }

        [Fact]
        public void When_HandleSwitchFloating_And_FocusNodeIsFloating_Then_ChangeItToTile()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            //var mockSut = new Mock<VirtualDesktop>(1, null, null, new RECT(10, 10, 30, 40), Direction.Horizontal, new ScreenNode[] { screen.Object }) { CallBase = true };
            var screen = NodeHelper.CreateMockScreen();
            var mockSut = CreateMockSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: new ScreenNode[] { screen.Object });
            var sut = mockSut.Object;
            sut.AddFloatingNode(child.Object);
            focusTracker.Setup(m => m.FocusNode()).Returns(child.Object);
            child.Invocations.Clear();
            mockSut.Invocations.Clear();

            // Act
            sut.HandleSwitchFloating();

            // Assert
            mockSut.Verify(m => m.DisconnectChild(child.Object));
            child.VerifySet(m => m.Parent = screen.Object);
            child.VerifySet(m => m.Style = NodeStyle.Tile);
            sut.FloatingNodes.Should().NotContain(child.Object);
        }
#endregion
#region MoveFloatingNodeToDesktop
        [Fact]
        public void When_TransferFocusNodeToDesktop_And_NodeIsFloating_Then_DoTheMove()
        {
            // Arrange
            var floatingNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var screenSut = NodeHelper.CreateMockScreen();
            var screenDest = NodeHelper.CreateMockScreen();
            var sut = CreateSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: new ScreenNode[]{ screenSut.Object});
            var destination = CreateMockSut(childs: new ScreenNode[]{ screenDest.Object});
            sut.AddFloatingNode(floatingNode.Object);
            destination.Setup(m => m.AddNodes(floatingNode.Object)).Returns(true).Verifiable("AddNodes should be called when moving a node");
            focusTracker.Setup(m => m.FocusNode()).Returns(floatingNode.Object);
            sut.FloatingNodes.Should().Contain((floatingNode.Object));

            // Act
            sut.TransferFocusNodeToDesktop(destination.Object);

            // Assert
            destination.VerifyAll();
            sut.FloatingNodes.Should().NotContain((floatingNode.Object));
        }

        [Fact]
        public void When_TransferFocusNodeToDesktop_And_NodeIsFloating_And_DisconnectChildReturnFalse_Then_Abort()
        {
            // Arrange
            var floatingNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var screenSut = NodeHelper.CreateMockScreen();
            var screenDest = NodeHelper.CreateMockScreen();
            var mockSut = CreateMockSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: new ScreenNode[]{ screenSut.Object});
            var sut = mockSut.Object;
            var destination = CreateMockSut(childs: new ScreenNode[]{ screenDest.Object});
            sut.AddFloatingNode(floatingNode.Object);
            mockSut.Setup(m => m.DisconnectChild(floatingNode.Object)).Returns(false).Verifiable();
            focusTracker.Setup(m => m.FocusNode()).Returns(floatingNode.Object);

            // Act
            sut.TransferFocusNodeToDesktop(destination.Object);

            // Assert
            destination.Verify(m => m.AddNodes(It.IsAny<Node>()), Times.Never());
        }

        [Fact]
        public void When_TransferFocusNodeToDesktop_And_NodeIsFloating_And_EverythingGoOk_Then_UntrackNode()
        {
            // Arrange
            var floatingNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var screenSut = NodeHelper.CreateMockScreen();
            var screenDest = NodeHelper.CreateMockScreen();
            var sut = CreateSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: new ScreenNode[]{ screenSut.Object});
            var destination = CreateMockSut(childs: new ScreenNode[]{ screenDest.Object});
            sut.AddFloatingNode(floatingNode.Object);
            destination.Setup(m => m.AddNodes(floatingNode.Object)).Returns(true).Verifiable("AddNodes should be called when moving a node");
            focusTracker.Setup(m => m.FocusNode()).Returns(floatingNode.Object);
            sut.FloatingNodes.Should().Contain((floatingNode.Object));

            // Act
            sut.TransferFocusNodeToDesktop(destination.Object);

            // Assert
            focusTracker.Verify(m => m.Untrack(floatingNode.Object));
        }
#endregion
#region DisconnectChild
        [Fact]
        public void When_DisconnectChild_And_NodeIsFloating_Then_DisconnectListeners()
        {
            // Arrange
            var floatingNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var screenSut = NodeHelper.CreateMockScreen();
            var sut = CreateSut(childs: new ScreenNode[]{ screenSut.Object});
            sut.AddFloatingNode(floatingNode.Object);

            // Act
            sut.DisconnectChild(floatingNode.Object);

            // Assert
            floatingNode.Object.Parent.Should().BeNull();
        }
#endregion
#region RemoveChild
        [Fact]
        public void When_RemoveChild_And_Floating_Then_DisconnectIt()
        {
            // Arrange
            var mockSut = CreateMockSut(childs: new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            var sut = mockSut.Object;
            var child = NodeHelper.CreateMockNode();
            child.SetupGet(m => m.Style).Returns(NodeStyle.Floating);

            // Act
            sut.RemoveChild(child.Object);

            // Assert
            mockSut.Verify(m => m.DisconnectChild(child.Object));
        }
        [Fact]
        public void When_RemoveChild_And_Floating_Then_DisposeChild()
        {
            // Arrange
            var mockSut = CreateMockSut(childs: new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            var sut = mockSut.Object;
            var child = NodeHelper.CreateMockNode();
            child.SetupGet(m => m.Style).Returns(NodeStyle.Floating);
            mockSut.Setup(m => m.DisconnectChild(child.Object)).Returns(true);

            // Act
            sut.RemoveChild(child.Object);

            // Assert
            child.Verify(m => m.Dispose());
        }
#endregion
#region Show
        [Fact]
        public void When_Show_Then_ShowFloatingNodes()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut(childs: new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            child.SetupGet(m => m.Style).Returns(NodeStyle.Floating);
            sut.AddFloatingNode(child.Object);

            // Act
            child.Invocations.Clear();
            sut.Show();

            // Assert
            child.Verify(m => m.Show());
        }

#endregion
#region Hide
        [Fact]
        public void When_Hide_Then_HideFloatingNodes()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut(childs: new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            child.SetupGet(m => m.Style).Returns(NodeStyle.Floating);
            sut.AddFloatingNode(child.Object);

            // Act
            child.Invocations.Clear();
            sut.Hide();

            // Assert
            child.Verify(m => m.Hide());
        }

#endregion
#region ScreensChanged
        [Fact]
        public void When_ScreenChanged_And_LessScreens_Then_TransferChilds_And_RemoveScreens()
        {
            // Arrange
            var keep = NodeHelper.CreateMockScreen();
            var remove = NodeHelper.CreateMockScreen();
            var sut = CreateSut(childs: new ScreenNode[] { keep.Object, remove.Object });
            var screens = new RECT[] { new RECT(0, 0, 10, 10) };
            var direction = Direction.Horizontal;

            // Act
            sut.ScreensChanged(screens, direction);

            // Assert
            keep.Verify(m => m.TransferAllChilds(It.IsAny<ScreenNode>(), It.IsAny<TransferDirection>()), Times.Never());
            keep.Verify(m => m.Dispose(), Times.Never());
            remove.Verify(m => m.TransferAllChilds(It.IsAny<ScreenNode>(), It.IsAny<TransferDirection>()), Times.Once());
            remove.Verify(m => m.Dispose(), Times.Once());
        }

        [Fact]
        public void When_ScreenChanged_Then_UpdateRectOnlyIfNeeded()
        {
            // Arrange
            var noUpdate = NodeHelper.CreateMockScreen(rect: new RECT(0, 0, 10, 10));
            var update = NodeHelper.CreateMockScreen(rect: new RECT(10, 0, 20, 10));
            var sut = CreateSut(rect: new RECT(0, 0, 30, 10), childs: new ScreenNode[] { noUpdate.Object, update.Object });
            var screens = new RECT[] { noUpdate.Object.Rect, new RECT(0, 0, 20, 20) };
            var direction = Direction.Horizontal;
            noUpdate.Invocations.Clear();
            update.Invocations.Clear();

            // Act
            sut.ScreensChanged(screens, direction);

            // Assert
            noUpdate.Verify(m => m.UpdateRect(It.IsAny<RECT>()), Times.Never());
            update.Verify(m => m.UpdateRect(screens[1]), Times.Once());
        }

        [Fact]
        public void When_ScreenChanged_And_ThereIsANewScreen_Then_AddTheNewScreen()
        {
            // Arrange
            var existing = NodeHelper.CreateMockScreen(direction: Direction.Horizontal);
            var newNode = NodeHelper.CreateMockScreen(direction: Direction.Vertical);
            var sut = CreateSut(screenCreater: out Mock<IScreenNodeCreater> screenCreater, rect: new RECT(0, 0, 30, 10), childs: new ScreenNode[] { existing.Object });
            var screens = new RECT[] { existing.Object.Rect, new RECT(0, 0, 20, 20) };
            var direction = Direction.Horizontal;

            screenCreater.Setup(m => m.Create(It.IsAny<string>(), screens[1], direction)).Returns(newNode.Object).Verifiable();

            // Act
            sut.ScreensChanged(screens, direction);

            // Assert
            screenCreater.VerifyAll();
            sut.Childs.Should().Contain(newNode.Object);
        }
#endregion
#region helpers
        private VirtualDesktop CreateSut(out Mock<IScreenNodeCreater> screenCreater, RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            return CreateMockSut(out screenCreater, out _, out _, out _, rect, direction, childs, callPostInit).Object;
        }
        private VirtualDesktop CreateSut(out Mock<FocusTracker> focusTracker, RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            return CreateMockSut(out _, out focusTracker, out _, out _, rect, direction, childs, callPostInit).Object;
        }
        private VirtualDesktop CreateSut(RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            return CreateMockSut(out _, out _, rect, direction, childs, callPostInit).Object;
        }
        private VirtualDesktop CreateSut(out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null)
        {
            return CreateMockSut(out containerCreater, out windowTracker, rect, direction, childs).Object;
        }

        private Mock<VirtualDesktop> CreateMockSut(out Mock<FocusTracker> focusTracker, RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            return CreateMockSut(out _, out focusTracker, out _, out _, rect, direction, childs, callPostInit);
        }

        private Mock<VirtualDesktop> CreateMockSut(RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            return CreateMockSut(out _, out _, rect, direction, childs, callPostInit);
        }

        private Mock<VirtualDesktop> CreateMockSut(out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            return CreateMockSut(out _, out _, out containerCreater, out windowTracker, rect, direction, childs, callPostInit);
        }

        private Mock<VirtualDesktop> CreateMockSut(out Mock<IScreenNodeCreater> screenCreater, out Mock<FocusTracker> focusTracker, out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Direction direction = Direction.Horizontal, ScreenNode[] childs = null, bool callPostInit = true)
        {
            focusTracker = new Mock<FocusTracker>() { CallBase = true };
            containerCreater = new Mock<IContainerNodeCreater>();
            windowTracker = new Mock<IWindowTracker>();
            screenCreater = new Mock<IScreenNodeCreater>();

            var desktop = new Mock<VirtualDesktop>(1,
                screenCreater.Object,
                focusTracker.Object,
                containerCreater.Object,
                windowTracker.Object,
                rect ?? new RECT(10, 20, 30, 40),
                direction) { CallBase = true };
            if (callPostInit)
                desktop.Object.PostInit(childs);
            return desktop;
        }
#endregion
    }
}