using System;
using System.Reflection;
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
    public class ContainerNodeTests
    {
#region ctor
        [Fact]
        public void When_Initialize_Then_AddAndTakeOverChilds()
        {
            // Arrange
            var childs = new Node[]
            {
                CreateSut(),
                CreateSut()
            };

            // Act
            var sut = CreateSut(childs: childs);

            // Assert
            var styleBound1 = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[0]) as Delegate;
            var styleBound2 = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[1]) as Delegate;
            var requestBound1 = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[0]) as Delegate;
            var requestBound2 = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[1]) as Delegate;

            childs[0].Parent.Should().Equals(sut);
            childs[1].Parent.Should().Equals(sut);
            styleBound1.Should().NotBeNull();
            styleBound2.Should().NotBeNull();
            requestBound1.Should().NotBeNull();
            requestBound2.Should().NotBeNull();
        }

        [Fact]
        public void When_Initialize_Then_UpdateRenderer()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child1.Object, child2.Object };

            // Act
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs);

            // Assert
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }

        [Fact]
        public void When_Initialize_And_ChildRequireBiggerRect_Then_RequestBiggerRectForSelf()
        {
            // Arrange
            var smallRect = new RECT(10, 10, 20, 20);
            var biggerRect = new RECT(10, 10, 200, 200);
            var child1 = new Mock<Node>(biggerRect, Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child1.Object, child2.Object };
            var requestRectChangeTriggered = false;
            child1.Object.FixedRect = true;
            child1.SetupGet(m => m.Rect).Returns(biggerRect);
            child1.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            var sut = CreateSut(callPostInit: false);
            sut.RequestRectChange += (sender, arg) => requestRectChangeTriggered = true;

            // Act
            sut.PostInit(childs);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
#endregion
#region ChildNodeStyleChange
        [Fact]
        public void When_ChildTriggerStyleChange_Then_TriggerStyleChanged()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var childs = new Node[] { child1.Object };
            var styleChangedTriggered = false;
            var sut = CreateSut(childs: childs);
            sut.StyleChanged += (sender, args) => styleChangedTriggered = true;

            // Act
            child1.Object.Style = NodeStyle.FullscreenOne;

            // Assert
            styleChangedTriggered.Should().BeTrue();
        }
#endregion
#region AddWindow
        [Fact]
        public void When_AddWindow_And_FocusNodeCanHaveChilds_Then_CallFocusNodesAddWindow()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var childs = new Node[] { child1.Object };
            var hWnd = new IntPtr(666);
            var sut = CreateSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: childs);
            child1.SetupGet(m => m.CanHaveChilds).Returns(true);
            focusTracker.Setup(m => m.MyLastFocusNode(sut)).Returns(child1.Object);

            // Act
            sut.AddWindow(hWnd);

            // Assert
            child1.Verify(m => m.AddWindow(hWnd));
        }

        [Fact]
        public void When_AddWindow_And_FocusNodeCannotHaveChilds_Then_InsertNewNodeAfterFocusNode()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child1.Object, child2.Object };
            var hWnd = new IntPtr(666);
            var sut = CreateSut(focusTracker: out Mock<FocusTracker> focusTracker, windowTracker: out Mock<IWindowTracker> windowTracker, childs: childs);
            child1.SetupGet(m => m.CanHaveChilds).Returns(false);
            focusTracker.Setup(m => m.MyLastFocusNode(sut)).Returns(child1.Object);
            windowTracker.Setup(m => m.CreateNode(hWnd, true)).Returns(NodeHelper.CreateWindowNode(hWnd));
            
            // Act
            sut.AddWindow(hWnd);

            // Assert
            sut.Childs[1].As<WindowNode>().Hwnd.Equals(hWnd);
        }

        [Fact]
        public void When_AddWindow_And_NoFocusNode_Then_InsertNewNodeAtEnd()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child1.Object, child2.Object };
            var hWnd = new IntPtr(666);
            var sut = CreateSut(containerCreater: out _, windowTracker: out Mock<IWindowTracker> windowTracker, childs: childs);
            windowTracker.Setup(m => m.CreateNode(hWnd, true)).Returns(NodeHelper.CreateWindowNode(hWnd));
            
            // Act
            sut.AddWindow(hWnd);

            // Assert
            sut.Childs[2].As<WindowNode>().Hwnd.Equals(hWnd);
        }

        [Fact]
        public void When_AddWindow_And_NewRectIsBigger_Then_RequestRectChange()
        {
            // Arrange
            var origRect = new RECT(10, 10, 20, 20);
            var bigRect = new RECT(10, 10, 30, 30);
            var child1 = new Mock<Node>(origRect, Direction.Horizontal, null);
            var childs = new Node[] { child1.Object };
            var hWnd = new IntPtr(666);
            var sut = CreateSut(containerCreater: out _, windowTracker: out Mock<IWindowTracker> windowTracker, rect: origRect, childs: childs);
            var requestRectChangeTriggered = false;
            child1.Object.FixedRect = true;
            child1.SetupGet(m => m.Rect).Returns(bigRect);
            child1.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns<RECT>(bigRect.Equals);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;
            windowTracker.Setup(m => m.CreateNode(hWnd, true)).Returns(NodeHelper.CreateWindowNode(hWnd));

            // Act
            sut.AddWindow(hWnd);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }

#endregion
#region AddNodes
        [Fact]
        public void When_AddNodes_Then_TakeOverAllNodes()
        {
            // Arrange
            var childs = new Node[]
            {
                CreateSut(),
                CreateSut()
            };
            var sut = CreateSut();

            // Act
            var result = sut.AddNodes(childs);

            // Assert
            var styleBound1 = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[0]) as Delegate;
            var styleBound2 = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[1]) as Delegate;
            var requestBound1 = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[0]) as Delegate;
            var requestBound2 = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(childs[1]) as Delegate;

            result.Should().BeTrue();
            childs[0].Parent.Should().Equals(sut);
            childs[1].Parent.Should().Equals(sut);
            styleBound1.Should().NotBeNull();
            styleBound2.Should().NotBeNull();
            requestBound1.Should().NotBeNull();
            requestBound2.Should().NotBeNull();
        }
        [Fact]
        public void When_AddNodes_And_ContainerIsVisible_Then_CallNodesShow()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.Show();

            // Act
            sut.AddNodes(child.Object);

            // Assert
            child.Verify(m => m.Show());
        }
        [Fact]
        public void When_AddNodes_And_ContainerIsNotVisible_Then_CallNodesHide()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var sut = CreateSut();
            sut.Hide();

            // Act
            sut.AddNodes(child.Object);

            // Assert
            child.Verify(m => m.Hide());
        }

        [Fact]
        public void When_AddNodes_And_RectTooBig_Then_RequestChangeRect()
        {
            // Arrange
            var smallRect = new RECT(10, 10, 20, 20);
            var biggerRect = new RECT(10, 10, 200, 200);
            var child1 = new Mock<Node>(biggerRect, Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var requestRectChangeTriggered = false;
            var sut = CreateSut(rect: smallRect);
            child1.SetupGet(m => m.Rect).Returns(biggerRect);
            child1.Object.FixedRect = true;
            child1.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;
            
            // Act
            sut.AddNodes(child1.Object, child2.Object);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        [Fact]
        public void When_AddNodes_Then_PreUpdate_And_UpdateOnRenderer()
        {
            // Arrange
            var existingChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var newChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var existingChilds = new Node[] { existingChild.Object };
            var newChilds = new Node[] { newChild.Object };
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: existingChilds);
            existingChild.Invocations.Clear();

            // Act
            sut.AddNodes(newChilds);

            // Assert
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }
#endregion
#region ChangeDirection
        [Fact]
        public void When_ChangeDirection_Then_ChangeDirection_And_UpdateRenderer()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ child.Object };
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs, direction: Direction.Horizontal);

            // Act
            sut.ChangeDirection(Direction.Vertical);

            // Assert
            sut.Direction.Should().Be(Direction.Vertical);
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }
        [Fact]
        public void When_ChangeDirection_And_NewRectTooBig_Then_TriggerRequestRectChange()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 100, 200);
            
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ child.Object };
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal, rect: smallRect);
            child.SetupGet(m => m.Rect).Returns(bigRect);
            child.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            sut.RequestRectChange += (sender, arg) => requestRectChangeTriggered = true;

            // Act
            sut.ChangeDirection(Direction.Vertical);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }

#endregion
#region SetFocus
        [Fact]
        public void When_SetFocus_And_DirectionIsLeft_Then_SetFocusToLastChild()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ child1.Object, child2.Object };
            var sut = CreateSut(childs: childs);

            // Act
            sut.SetFocus(TransferDirection.Left);

            // Assert
            child2.Verify(m => m.SetFocus(TransferDirection.Left));
        }
        [Fact]
        public void When_SetFocus_And_DirectionIsUp_Then_SetFocusToLastChild()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ child1.Object, child2.Object };
            var sut = CreateSut(childs: childs);

            // Act
            sut.SetFocus(TransferDirection.Up);

            // Assert
            child2.Verify(m => m.SetFocus(TransferDirection.Up));
        }
        [Fact]
        public void When_SetFocus_And_DirectionIsRight_Then_SetFocusToFirstChild()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ child1.Object, child2.Object };
            var sut = CreateSut(childs: childs);

            // Act
            sut.SetFocus(TransferDirection.Right);

            // Assert
            child1.Verify(m => m.SetFocus(TransferDirection.Right));
        }
        [Fact]
        public void When_SetFocus_And_DirectionIsDown_Then_SetFocusToFirstChild()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var child2 = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ child1.Object, child2.Object };
            var sut = CreateSut(childs: childs);

            // Act
            sut.SetFocus(TransferDirection.Down);

            // Assert
            child1.Verify(m => m.SetFocus(TransferDirection.Down));
        }
#endregion
#region FocusNodeInDirection
        // Bug #98, couldn't move focus if screen container was empty
        [Fact]
        public void When_FocusNodeIsSelf_Then_LetParentDecide()
        {
            // Arrange
            var childs = new Node[0];
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs);

            // Act
            var result = sut.FocusNodeInDirection(sut, TransferDirection.Up);

            // Assert
            parent.Verify(m => m.FocusNodeInDirection(sut, TransferDirection.Up));
        }

        [Fact]
        public void When_FocusNodeInDirection_And_FocusNodeIsFullscreen_Then_LetParentNodeDecide()
        {
            // Arrange
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs);

            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.FullscreenOne);
            parent.Setup(m => m.FocusNodeInDirection(focusNode.Object, TransferDirection.Up)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, TransferDirection.Up);

            // Assert
            result.Should().BeTrue("Because parents FocusNodeInDirection returns true");
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_FocusNodeIsFullscreen_And_ParentIsNull_Then_ReturnFalse()
        {
            // Arrange
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(childs: childs);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.FullscreenOne);
            sut.Parent = null;

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, TransferDirection.Up);

            // Assert
            result.Should().BeFalse();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_FocusNodeIsNotAChildOfContainerNode_Then_ReturnFalse()
        {
            // Arrange
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[0];
            var sut = CreateSut(childs: childs);

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, TransferDirection.Up);

            // Assert
            result.Should().BeFalse();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsLeft_And_DirectionIsVertical_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsLeft_And_ChildCountIsOne_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsLeft_And_FocusNodeIsFirstInList_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object, CreateSut() };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsLeft_Then_FocusChildNodeToTheLeft()
        {
            // Arrange
            var direction = TransferDirection.Left;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var newFocusNode = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { newFocusNode.Object, focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(It.IsAny<Node>(), It.IsAny<TransferDirection>())).Throws(new Exception("Should not be called"));

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            newFocusNode.Verify(m => m.SetFocus(direction));
        }



        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsUp_And_DirectionIsHorizontal_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Up;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsUp_And_ChildCountIsOne_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Up;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsUp_And_FocusNodeIsFirstInList_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Up;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object, CreateSut() };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsUp_Then_FocusChildNodeToTheUp()
        {
            // Arrange
            var direction = TransferDirection.Up;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var newFocusNode = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { newFocusNode.Object, focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(It.IsAny<Node>(), It.IsAny<TransferDirection>())).Throws(new Exception("Should not be called"));

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            newFocusNode.Verify(m => m.SetFocus(direction));
        }




        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsRight_And_DirectionIsVertical_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Right;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsRight_And_ChildCountIsOne_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Right;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsRight_And_FocusNodeIsLastInList_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Right;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ CreateSut(), focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsRight_Then_FocusChildNodeToTheRight()
        {
            // Arrange
            var direction = TransferDirection.Right;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var newFocusNode = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusNode.Object, newFocusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(It.IsAny<Node>(), It.IsAny<TransferDirection>())).Throws(new Exception("Should not be called"));

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            newFocusNode.Verify(m => m.SetFocus(direction));
        }





        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsDown_And_DirectionIsHorizontal_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Down;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsDown_And_ChildCountIsOne_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Down;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsDown_And_FocusNodeIsLastInList_Then_CallParent()
        {
            // Arrange
            var direction = TransferDirection.Down;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[]{ CreateSut(), focusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(sut, direction)).Returns(true).Verifiable();

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            parent.VerifyAll();
        }
        [Fact]
        public void When_FocusNodeInDirection_And_TransDirectionIsDown_Then_FocusChildNodeToTheDown()
        {
            // Arrange
            var direction = TransferDirection.Down;
            var focusNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var newFocusNode = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusNode.Object, newFocusNode.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            focusNode.SetupGet(m => m.Style).Returns(NodeStyle.Tile);
            parent.Setup(m => m.FocusNodeInDirection(It.IsAny<Node>(), It.IsAny<TransferDirection>())).Throws(new Exception("Should not be called"));

            // Act
            var result = sut.FocusNodeInDirection(focusNode.Object, direction);

            // Assert
            result.Should().BeTrue();
            newFocusNode.Verify(m => m.SetFocus(direction));
        }

#endregion
#region TransferNode
        [Fact]
        public void When_TransferNode_And_ChildIsNotChildOfContainerNode_Then_ReturnFalse()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = CreateSut();
            var transDir = TransferDirection.Left;
            var gotFocus = false;
            var sut = CreateSut(childs: new Node[0]);

            // Act
            var result = sut.TransferNode(child, transferNode, transDir, gotFocus);

            // Assert
            result.Should().BeFalse();
        }

        #region WRong direction tests
        [Fact]
        public void When_TransferNodeFromChildNode_And_DirectionIsVertical_And_TransfDirectionIsLeft_Then_LetParentHandleTransfer()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = CreateSut();
            var transDir = TransferDirection.Left;
            var gotFocus = false;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child }, direction: Direction.Vertical);
            parent.Setup(m => m.TransferNode(sut, transferNode, transDir, gotFocus)).Returns(true).Verifiable();

            // Act
            var result = sut.TransferNode(child, transferNode, transDir, gotFocus);

            // Assert
            result.Should().BeTrue("Because parent returns true");
            parent.VerifyAll();
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_DirectionIsVertical_And_TransfDirectionIsRight_Then_LetParentHandleTransfer()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = CreateSut();
            var transDir = TransferDirection.Right;
            var gotFocus = false;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child }, direction: Direction.Vertical);
            parent.Setup(m => m.TransferNode(sut, transferNode, transDir, gotFocus)).Returns(true).Verifiable();

            // Act
            var result = sut.TransferNode(child, transferNode, transDir, gotFocus);

            // Assert
            result.Should().BeTrue("Because parent returns true");
            parent.VerifyAll();
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_DirectionIsHorizontal_And_TransfDirectionIsUp_Then_LetParentHandleTransfer()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = CreateSut();
            var transDir = TransferDirection.Up;
            var gotFocus = false;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child }, direction: Direction.Horizontal);
            parent.Setup(m => m.TransferNode(sut, transferNode, transDir, gotFocus)).Returns(true).Verifiable();

            // Act
            var result = sut.TransferNode(child, transferNode, transDir, gotFocus);

            // Assert
            result.Should().BeTrue("Because parent returns true");
            parent.VerifyAll();
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_DirectionIsHorizontal_And_TransfDirectionIsDown_Then_LetParentHandleTransfer()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = CreateSut();
            var transDir = TransferDirection.Down;
            var gotFocus = false;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child }, direction: Direction.Horizontal);
            parent.Setup(m => m.TransferNode(sut, transferNode, transDir, gotFocus)).Returns(true).Verifiable();

            // Act
            var result = sut.TransferNode(child, transferNode, transDir, gotFocus);

            // Assert
            result.Should().BeTrue("Because parent returns true");
            parent.VerifyAll();
        }
        #endregion
        #region TransferDirection.Left
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsLeft_Then_TakeOverTransferNode()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Left;
            var gotFocus = false;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: new Node[] { child }, direction: Direction.Horizontal);

            // Act
            var result = sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;

            result.Should().BeTrue();
            transferNode.VerifySet(m => m.Parent = sut, "Parent should be the new containerNode");
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
            styleBound.Should().NotBeNull("StyleChanged should have an listener on it");
            requestBound.Should().NotBeNull("RequestRectChange should have an listener on it");
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsLeft_And_UpdateRectReturnsWrongSize_Then_TriggerRequestRectEvent()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = CreateSut();
            var transferNode = new Mock<Node>(bigRect, Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Left;
            var gotFocus = false;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: new Node[] { child }, direction: Direction.Horizontal, rect: smallRect);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;
            transferNode.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            transferNode.SetupGet(m => m.Rect).Returns(bigRect);

            // Act
            sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
        #region TransferDirection.Up
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsUp_Then_TakeOverTransferNode()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Up;
            var gotFocus = false;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: new Node[] { child }, direction: Direction.Vertical);

            // Act
            var result = sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;

            result.Should().BeTrue();
            transferNode.VerifySet(m => m.Parent = sut, "Parent should be the new containerNode");
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
            styleBound.Should().NotBeNull("StyleChanged should have an listener on it");
            requestBound.Should().NotBeNull("RequestRectChange should have an listener on it");
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsUp_And_UpdateRectReturnsWrongSize_Then_TriggerRequestRectEvent()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = CreateSut();
            var transferNode = new Mock<Node>(bigRect, Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Up;
            var gotFocus = false;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: new Node[] { child }, direction: Direction.Vertical, rect: smallRect);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;
            transferNode.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            transferNode.SetupGet(m => m.Rect).Returns(bigRect);

            // Act
            sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
        #region TransferDirection.Right
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsRight_Then_TakeOverTransferNode()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Right;
            var gotFocus = false;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: new Node[] { child }, direction: Direction.Horizontal);

            // Act
            var result = sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;

            result.Should().BeTrue();
            transferNode.VerifySet(m => m.Parent = sut, "Parent should be the new containerNode");
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
            styleBound.Should().NotBeNull("StyleChanged should have an listener on it");
            requestBound.Should().NotBeNull("RequestRectChange should have an listener on it");
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsRight_And_UpdateRectReturnsWrongSize_Then_TriggerRequestRectEvent()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = CreateSut();
            var transferNode = new Mock<Node>(bigRect, Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Right;
            var gotFocus = false;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: new Node[] { child }, direction: Direction.Horizontal, rect: smallRect);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;
            transferNode.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            transferNode.SetupGet(m => m.Rect).Returns(bigRect);

            // Act
            sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
        #region TransferDirection.Down
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsDown_Then_TakeOverTransferNode()
        {
            // Arrange
            var child = CreateSut();
            var transferNode = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Down;
            var gotFocus = false;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: new Node[] { child }, direction: Direction.Vertical);
            renderer.Invocations.Clear();

            // Act
            var result = sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transferNode.Object) as Delegate;

            result.Should().BeTrue();
            transferNode.VerifySet(m => m.Parent = sut, "Parent should be the new containerNode");
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
            styleBound.Should().NotBeNull("StyleChanged should have an listener on it");
            requestBound.Should().NotBeNull("RequestRectChange should have an listener on it");
        }
        [Fact]
        public void When_TransferNodeFromChildNode_And_TransDirIsDown_And_UpdateRectReturnsWrongSize_Then_TriggerRequestRectEvent()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = CreateSut();
            var transferNode = new Mock<Node>(bigRect, Direction.Horizontal, null) { CallBase = true };
            var transDir = TransferDirection.Down;
            var gotFocus = false;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: new Node[] { child }, direction: Direction.Vertical, rect: smallRect);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;
            transferNode.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            transferNode.SetupGet(m => m.Rect).Returns(bigRect);

            // Act
            sut.TransferNode(child, transferNode.Object, transDir, gotFocus);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
#endregion
#region ChildWantMove
        #region Left
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_Then_DoNotOverwriteParentPointer()
        {
            // Arrange
            var child = CreateSut();
            var transDir = TransferDirection.Left;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child });
            sut.Parent = parent.Object;
            child.Parent = parent.Object;

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            child.Parent.Should().BeEquivalentTo(parent.Object); // Bug #78
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_ChildIsFirstInArray_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var transDir = TransferDirection.Left;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child });
            sut.Parent = parent.Object;

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_DirectionIsVertical_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Left;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_ParentTransferNodeReturnsFalse_Then_DoNotRemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Left;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(false);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs[1].Should().Be(child);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_ParentTransferNodeReturnsTrue_Then_RemoveChildFromArray()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Left;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(true);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs.Count.Should().Be(1);
        }
        #endregion
        #region Up
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_Then_DoNotOverwriteParentPointer()
        {
            // Arrange
            var child = CreateSut();
            var transDir = TransferDirection.Up;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child });
            sut.Parent = parent.Object;
            child.Parent = parent.Object;

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            child.Parent.Should().BeEquivalentTo(parent.Object); // Bug #78
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_ChildIsFirstInArray_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var transDir = TransferDirection.Up;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child });
            sut.Parent = parent.Object;

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_DirectionIsHorizontal_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Up;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_ParentTransferNodeReturnsFalse_Then_DoNotRemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Up;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(false);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs[1].Should().Be(child);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_ParentTransferNodeReturnsTrue_Then_RemoveChildFromArray()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Up;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(true);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs.Count.Should().Be(1);
        }
        #endregion
        #region Right
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_Then_DoNotOverwriteParentPointer()
        {
            // Arrange
            var child = CreateSut();
            var transDir = TransferDirection.Right;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child });
            sut.Parent = parent.Object;
            child.Parent = parent.Object;

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            child.Parent.Should().BeEquivalentTo(parent.Object); // Bug #78
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_ChildIsLastInArray_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_DirectionIsVertical_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_ParentTransferNodeReturnsFalse_Then_DoNotRemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(false);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs[1].Should().Be(child);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_ParentTransferNodeReturnsTrue_Then_RemoveChildFromArray()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Vertical);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(true);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs.Count.Should().Be(1);
        }
        #endregion
        #region Down
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_Then_DoNotOverwriteParentPointer()
        {
            // Arrange
            var child = CreateSut();
            var transDir = TransferDirection.Down;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: new Node[] { child });
            sut.Parent = parent.Object;
            child.Parent = parent.Object;

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            child.Parent.Should().BeEquivalentTo(parent.Object); // Bug #78
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_ChildIsLastInArray_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_DirectionIsHorizontal_Then_TransferNodeToParent()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            parent.Verify(m => m.TransferNode(sut, child, transDir, false));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_ParentTransferNodeReturnsFalse_Then_DoNotRemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(false);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs[1].Should().Be(child);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_ParentTransferNodeReturnsTrue_Then_RemoveChildFromArray()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { CreateSut(), child };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs, direction: Direction.Horizontal);
            parent.Setup(m => m.TransferNode(sut, child, transDir, false)).Returns(true);

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            sut.Childs.Count.Should().Be(1);
        }
        #endregion
        #region Left
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_ChildToTheLeftCanHaveChildrenButReturnFalseOnTransfer_Then_TransferNodeToIt_And_DoNotRemoveChildDueToFalseOnTransfer()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { newParent.Object, child };
            var transDir = TransferDirection.Left;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(false).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Contains(child).Should().BeTrue();
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_ChildToTheLeftCanHaveChildrenButReturnTrueOnTransfer_Then_TransferNodeToIt_And_RemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { newParent.Object, child };
            var transDir = TransferDirection.Left;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(true).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Count.Should().Be(1);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_Then_InsertChildOneStepToLeft_And_UpdateRenderer()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { extraChild.Object, child.Object };
            var transDir = TransferDirection.Left;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs, direction: Direction.Horizontal);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            sut.Childs[0].Should().BeEquivalentTo(child.Object);
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsLeft_And_UpdateRectIsFalse_Then_ReqiestRectChange()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { extraChild.Object, child.Object };
            var transDir = TransferDirection.Left;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal, rect: smallRect);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);
            child.SetupGet(m => m.Rect).Returns(bigRect);
            child.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
        #region Up
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_ChildToTheUpCanHaveChildrenButReturnFalseOnTransfer_Then_TransferNodeToIt_And_DoNotRemoveChildDueToFalseOnTransfer()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { newParent.Object, child };
            var transDir = TransferDirection.Up;
            var sut = CreateSut(childs: childs, direction: Direction.Vertical);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(false).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Contains(child).Should().BeTrue();
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_ChildToTheUpCanHaveChildrenButReturnTrueOnTransfer_Then_TransferNodeToIt_And_RemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { newParent.Object, child };
            var transDir = TransferDirection.Up;
            var sut = CreateSut(childs: childs, direction: Direction.Vertical);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(true).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Count.Should().Be(1);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_Then_InsertChildOneStepToUp_And_UpdateRenderer()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { extraChild.Object, child.Object };
            var transDir = TransferDirection.Up;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs, direction: Direction.Vertical);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            sut.Childs[0].Should().BeEquivalentTo(child.Object);
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsUp_And_UpdateRectIsFalse_Then_ReqiestRectChange()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { extraChild.Object, child.Object };
            var transDir = TransferDirection.Up;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: childs, direction: Direction.Vertical, rect: smallRect);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);
            child.SetupGet(m => m.Rect).Returns(bigRect);
            child.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
        #region Right
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_ChildToTheRightCanHaveChildrenButReturnFalseOnTransfer_Then_TransferNodeToIt_And_DoNotRemoveChildDueToFalseOnTransfer()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child, newParent.Object };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(false).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Contains(child).Should().BeTrue();
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_ChildToTheRightCanHaveChildrenButReturnTrueOnTransfer_Then_TransferNodeToIt_And_RemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child, newParent.Object };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(true).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Count.Should().Be(1);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_Then_InsertChildOneStepToRight_And_UpdateRenderer()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child.Object, extraChild.Object };
            var transDir = TransferDirection.Right;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs, direction: Direction.Horizontal);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            sut.Childs[1].Should().BeEquivalentTo(child.Object);
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsRight_And_UpdateRectIsFalse_Then_ReqiestRectChange()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child.Object, extraChild.Object };
            var transDir = TransferDirection.Right;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: childs, direction: Direction.Horizontal, rect: smallRect);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);
            child.SetupGet(m => m.Rect).Returns(bigRect);
            child.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion
        #region Down
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_ChildToTheDownCanHaveChildrenButReturnFalseOnTransfer_Then_TransferNodeToIt_And_DoNotRemoveChildDueToFalseOnTransfer()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child, newParent.Object };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(childs: childs, direction: Direction.Vertical);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(false).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Contains(child).Should().BeTrue();
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_ChildToTheDownCanHaveChildrenButReturnTrueOnTransfer_Then_TransferNodeToIt_And_RemoveChild()
        {
            // Arrange
            var child = CreateSut();
            var newParent = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child, newParent.Object };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(childs: childs, direction: Direction.Vertical);
            newParent.SetupGet(m => m.CanHaveChilds).Returns(true);
            newParent.Setup(m => m.TransferNode(null, child, transDir, false)).Returns(true).Verifiable();

            // Act
            sut.ChildWantMove(child, transDir);

            // Assert
            newParent.VerifyAll();
            sut.Childs.Count.Should().Be(1);
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_Then_InsertChildOneStepToDown_And_UpdateRenderer()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child.Object, extraChild.Object };
            var transDir = TransferDirection.Down;
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs, direction: Direction.Vertical);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            sut.Childs[1].Should().BeEquivalentTo(child.Object);
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));
        }
        [Fact]
        public void When_ChildWantMove_And_TransDirIsDown_And_UpdateRectIsFalse_Then_ReqiestRectChange()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var bigRect = new RECT(1, 2, 300, 400);
            var child = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var extraChild = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var childs = new Node[] { child.Object, extraChild.Object };
            var transDir = TransferDirection.Down;
            var requestRectChangeTriggered = false;
            var sut = CreateSut(childs: childs, direction: Direction.Vertical, rect: smallRect);
            extraChild.SetupGet(m => m.CanHaveChilds).Returns(false);
            child.SetupGet(m => m.Rect).Returns(bigRect);
            child.Setup(m => m.UpdateRect(It.IsAny<RECT>())).Returns(false);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;

            // Act
            sut.ChildWantMove(child.Object, transDir);

            // Assert
            requestRectChangeTriggered.Should().BeTrue();
        }
        #endregion

#endregion
#region RemoveChild
        [Fact]
        public void When_RemoveChild_And_NodeIsNotChildOfContainerNode_Then_ReturnFalse()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[0];
            var sut = CreateSut(childs: childs);

            // Act
            var result = sut.RemoveChild(child);

            // Assert
            result.Should().BeFalse();
        }
        [Fact]
        public void When_RemoveChild_And_NodeIsChildOfContainerNode_Then_RemoveChildAndReturnTrue()
        {
            // Arrange
            var smallRect = new RECT(1, 2, 3, 4);
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var remainingChild = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var childs = new Node[] { remainingChild.Object, child.Object };
            var sut = CreateSut(focusTracker: out Mock<FocusTracker> focusTracker, childs: childs, rect: smallRect);
            var requestRectChangeTriggered = false;
            focusTracker.Setup(m => m.MyLastFocusNode(sut)).Returns(child.Object);
            sut.MyFocusNode.Should().BeEquivalentTo(child.Object);
            sut.RequestRectChange += (sender, args) => requestRectChangeTriggered = true;

            // Act
            sut.RemoveChild(child.Object);

            // Assert
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(child.Object) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(child.Object) as Delegate;

            focusTracker.Verify(m => m.ExplicitSetMyFocusNode(sut, remainingChild.Object), "Next child should be the new explicit focus for this container");
            child.Verify(m => m.Dispose());
            sut.Childs.Count.Should().Be(1);
            styleBound.Should().BeNull();
            requestBound.Should().BeNull();
            remainingChild.VerifyAll();
            requestRectChangeTriggered.Should().BeTrue("If remainig children cannot fit rect then trigger RectChange event");
        }
        [Fact]
        public void When_RemoveChild_And_ItIsLastChild_Then_CallParentsRemoveChild()
        {
            // Arrange
            var child = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var childs = new Node[] { child.Object };
            var sut = CreateSut(parent: out Mock<Node> parent, childs: childs);

            // Act
            sut.RemoveChild(child.Object);

            // Assert
            parent.Verify(m => m.RemoveChild(sut));
            sut.Childs.Count.Should().Be(0);
        }
#endregion
#region TransferNodeToAnotherDesktop
        [Fact]
        public void When_TransferNodeToAnotherDesktop_And_NodeIsNotInChilds_Then_ReturnFalse()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            var result = sut.TransferNodeToAnotherDesktop(CreateSut(), null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_TransferNodeToAnotherDesktop_And_NodeIsTransfered_Then_RemoveNodeFromOldChildsAndReturnTrue()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { child };
            //var dst = new Mock<VirtualDesktop>(1, null, null, null, new RECT(), Direction.Horizontal, new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            var dst = NodeHelper.CreateMockDesktop();
            var sut = CreateSut(childs: childs);
            dst.Setup(m => m.AddNodes(child)).Returns(true).Verifiable();

            // Act
            var result = sut.TransferNodeToAnotherDesktop(child, dst.Object);

            // Assert
            result.Should().BeTrue();
            sut.Childs.Count.Should().Be(0);
            dst.VerifyAll();
        }

        [Fact] // Bug: #81
        public void When_TransferNodeToAnotherDesktop_And_NodeIsTransfered_Then_MakeSureContainerNodeUpdatesRemainingNodes()
        {
            // Arrange
            var child = CreateSut();
            var remainig = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { child, remainig.Object };
            //var dst = new Mock<VirtualDesktop>(1, null, null, new RECT(), Direction.Horizontal, new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            var destination = NodeHelper.CreateMockDesktop();
            var sut = CreateSut(renderer: out Mock<IRenderer> renderer, childs: childs);
            destination.Setup(m => m.AddNodes(child)).Returns(true).Verifiable();
            remainig.Invocations.Clear();
            
            // Act
            var result = sut.TransferNodeToAnotherDesktop(child, destination.Object);

            // Assert
            renderer.Verify(m => m.PreUpdate(sut, sut.Childs));
            renderer.Verify(m => m.Update(It.IsAny<List<int>>()));

        }

        [Fact]
        public void When_TransferNodeToAnotherDesktop_And_NodeIsNotTransfered_Then_KeepOldChildAndReturnFalse()
        {
            // Arrange
            var child = CreateSut();
            var childs = new Node[] { child };
            //var dst = new Mock<VirtualDesktop>(1, null, null, new RECT(), Direction.Horizontal, new ScreenNode[] { NodeHelper.CreateMockScreen().Object });
            var dst = NodeHelper.CreateMockDesktop();
            var sut = CreateSut(childs: childs);
            dst.Setup(m => m.AddNodes(child)).Returns(false).Verifiable();

            // Act
            var result = sut.TransferNodeToAnotherDesktop(child, dst.Object);

            // Assert
            result.Should().BeFalse();
            sut.Childs.Count.Should().Be(1);
            dst.VerifyAll();
        }
#endregion
#region DisconnectChild
        [Fact]
        public void When_DisconnectChild_And_NodeIsNotChildOfContainerNode_Then_ReturnFalse()
        {
            // Arrange
            var child = CreateSut();
            var sut = CreateSut();

            // Act
            var result = sut.DisconnectChild(child);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_DisconnectChild_Then_RemoveChild_And_ReturnTrue()
        {
            // Arrange
            var child = CreateSut();
            var sut = CreateSut(childs: new Node[] { child });

            // Act
            var result = sut.DisconnectChild(child);

            // Assert
            result.Should().BeTrue();
            sut.Childs.Count.Should().Be(0);
            child.Parent.Should().BeNull();

            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(child) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(child) as Delegate;
            var focusBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(child) as Delegate;
            styleBound.Should().BeNull();
            requestBound.Should().BeNull();
            focusBound.Should().BeNull();
        }
#endregion
#region Show
        [Fact]
        public void When_Show_Then_CallShowOnAllChilds()
        {
            // Arrange
            var child1 = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var child2 = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { child1.Object, child2.Object };
            var sut = CreateSut(childs: childs);
            child1.Setup(m => m.Show()).Returns(true).Verifiable();
            child2.Setup(m => m.Show()).Returns(true).Verifiable();

            // Act
            var result = sut.Show();

            // Assert
            result.Should().BeTrue("Because all childs return true");
            child1.VerifyAll();
            child2.VerifyAll();
        }
        [Fact]
        public void When_Show_And_OneChildReturnFalse_Then_ReturnFalse()
        {
            // Arrange
            var focusChild = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var otherChild = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusChild.Object, otherChild.Object };
            var sut = CreateSut(childs: childs);
            focusChild.Setup(m => m.Show()).Returns(false).Verifiable();
            otherChild.Setup(m => m.Show()).Returns(true).Verifiable();

            // Act
            var result = sut.Show();

            // Assert
            result.Should().BeFalse();
            focusChild.VerifyAll();
            otherChild.VerifyAll();
        }
#endregion
#region Hide
        [Fact]
        public void When_Hide_Then_CallHideOnAllChilds()
        {
            // Arrange
            var focusChild = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var otherChild = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusChild.Object, otherChild.Object };
            var sut = CreateSut(childs: childs);
            focusChild.Setup(m => m.Hide()).Returns(true).Verifiable();
            otherChild.Setup(m => m.Hide()).Returns(true).Verifiable();

            // Act
            var result = sut.Hide();

            // Assert
            result.Should().BeTrue("Because all childs return true");
            focusChild.VerifyAll();
            otherChild.VerifyAll();
        }
        [Fact]
        public void When_Hide_And_OneChildReturnFalse_Then_ReturnFalse()
        {
            // Arrange
            var focusChild = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var otherChild = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusChild.Object, otherChild.Object };
            var sut = CreateSut(childs: childs);
            focusChild.Setup(m => m.Hide()).Returns(false).Verifiable();
            otherChild.Setup(m => m.Hide()).Returns(true).Verifiable();

            // Act
            var result = sut.Hide();

            // Assert
            result.Should().BeFalse();
            focusChild.VerifyAll();
            otherChild.VerifyAll();
        }
#endregion
#region Restore
        [Fact]
        public void When_Restore_Then_CallRestoreOnAllChilds_And_CallSetFocusOnFocusNode()
        {
            // Arrange
            var focusChild = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var otherChild = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusChild.Object, otherChild.Object };
            var sut = CreateSut(childs: childs);
            focusChild.Setup(m => m.Restore()).Returns(true).Verifiable();
            otherChild.Setup(m => m.Restore()).Returns(true).Verifiable();

            // Act
            var result = sut.Restore();

            // Assert
            result.Should().BeTrue("Because all childs return true");
            focusChild.VerifyAll();
            otherChild.VerifyAll();
        }
        [Fact]
        public void When_Restore_And_OneChildReturnFalse_Then_ReturnFalse()
        {
            // Arrange
            var focusChild = new Mock<Node>(new RECT(), Direction.Horizontal, null) { CallBase = true };
            var otherChild = new Mock<Node>(new RECT(), Direction.Vertical, null);
            var childs = new Node[] { focusChild.Object, otherChild.Object };
            var sut = CreateSut(childs: childs);
            focusChild.Setup(m => m.Restore()).Returns(false).Verifiable();
            otherChild.Setup(m => m.Restore()).Returns(true).Verifiable();

            // Act
            var result = sut.Restore();

            // Assert
            result.Should().BeFalse();
            focusChild.VerifyAll();
            otherChild.VerifyAll();
        }
#endregion

#region helpers
        private ContainerNode CreateSut(RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal, Node[] childs = null, bool callPostInit = true)
        {

            return CreateSut(out _, out _, out _, out _, out _, out _, rect, parent, direction, childs);
        }
        private ContainerNode CreateSut(out Mock<IRenderer> renderer, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal, Node[] childs = null, bool callPostInit = true)
        {

            return CreateSut(out renderer, out _, out _, out _, out _, out _, rect, parent, direction, childs);
        }

        private ContainerNode CreateSut(out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal, Node[] childs = null)
        {

            return CreateSut(out _, out _, out _, out _, out containerCreater, out windowTracker, rect, parent, direction, childs);
        }

        private ContainerNode CreateSut(out Mock<FocusTracker> focusTracker, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal, Node[] childs = null)
        {

            return CreateSut(out _, out _, out focusTracker,  out _, out _, out _, rect, parent, direction, childs);
        }

        private ContainerNode CreateSut(out Mock<Node> parent, RECT? rect = null, Direction direction = Direction.Horizontal, Node[] childs = null)
        {

            return CreateSut(out _, out parent, out _,  out _, out _, out _, rect, null, direction, childs);
        }

        private ContainerNode CreateSut(out Mock<FocusTracker> focusTracker, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal, Node[] childs = null)
        {

            return CreateSut(out _, out _, out focusTracker,  out _, out _, out windowTracker, rect, parent, direction, childs);
        }

        private ContainerNode CreateSut(out Mock<IRenderer> renderer, out Mock<Node> parentNode, out Mock<FocusTracker> focusTracker, out Mock<IVirtualDesktop> desktop, out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal, Node[] childs = null, bool callPostInit = true)
        {
            desktop = new Mock<IVirtualDesktop>();
            focusTracker = new Mock<FocusTracker>() { CallBase = true };
            parentNode = new Mock<Node>(new RECT(), Direction.Horizontal, null);
            var sut = NodeHelper.CreateContainerNode(out renderer, out containerCreater, out windowTracker,rect, parent, direction);
            sut.Parent = parentNode.Object;
            parentNode.SetupGet(m => m.Desktop).Returns(desktop.Object);
            desktop.SetupGet(m => m.FocusTracker).Returns(focusTracker.Object);
            if (callPostInit)
                sut.PostInit(childs);
            return sut;
        }

#endregion
    }
}