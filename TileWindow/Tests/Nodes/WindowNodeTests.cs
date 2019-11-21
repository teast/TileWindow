using Xunit;
using FluentAssertions;
using TileWindow.Nodes;
using System;
using Moq;
using TileWindow.Handlers;
using System.Runtime.InteropServices;
using System.Text;
using static TileWindow.PInvoker;
using TileWindow.Trackers;

namespace TileWindow.Tests.Nodes
{
    public class WindowNodeTests
    {
#region ctor
        [Fact]
        public void When_Initialize_With_hWnd_Then_AddWindow_To_Tracker()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var dragHandler = new Mock<IDragHandler>();
            var focusHandler = new Mock<IFocusHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            var windowHandler = new Mock<IWindowEventHandler>();
            var windowTracker = new Mock<IWindowTracker>();
            var pinvokeHandler = new Mock<IPInvokeHandler>();
            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);

            // Arrange & Act
            var sut = new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, new RECT(10, 20, 30, 40), hWnd);

            // Assert
            windowTracker.Verify(m => m.AddWindow(new IntPtr(1), sut));
        }

        [Fact]
        public void When_Initialize_With_hWnd_Then_ListenToDragHandlerEvents()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var dragHandler = new Mock<IDragHandler>();
            var focusHandler = new Mock<IFocusHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            var windowHandler = new Mock<IWindowEventHandler>();
            var windowTracker = new Mock<IWindowTracker>();
            var pinvokeHandler = new Mock<IPInvokeHandler>();
            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);
            dragHandler.SetupAdd(m => m.OnDragEnd += (sender, args) => { });
            dragHandler.SetupAdd(m => m.OnDragStart += (sender, args) => { });
            dragHandler.SetupAdd(m => m.OnDragMove += (sender, args) => { });

            // Act
            var sut = new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, new RECT(10, 20, 30, 40), hWnd);

            // Assert
            dragHandler.VerifyAdd(m => m.OnDragEnd += It.IsAny<EventHandler<DragEndEvent>>(), Times.Once);
            dragHandler.VerifyAdd(m => m.OnDragStart += It.IsAny<EventHandler<DragStartEvent>>(), Times.Once);
            dragHandler.VerifyAdd(m => m.OnDragMove += It.IsAny<EventHandler<DragMoveEvent>>(), Times.Once);
        }

        [Fact]
        public void When_Initialize_With_hWnd_Then_SetWindowPos_With_Rect()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var dragHandler = new Mock<IDragHandler>();
            var focusHandler = new Mock<IFocusHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            var windowHandler = new Mock<IWindowEventHandler>();
            var windowTracker = new Mock<IWindowTracker>();
            var pinvokeHandler = new Mock<IPInvokeHandler>();
            var rect = new RECT(10, 20, 30, 40);
            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);

            // Arrange & Act
            var sut = new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, rect, hWnd);

            // Assert
            pinvokeHandler.Verify(m => m.SetWindowPos(new IntPtr(1), IntPtr.Zero, rect.Left, rect.Top, rect.Right-rect.Left, rect.Bottom-rect.Top, It.IsAny<SetWindowPosFlags>()));
        }
#endregion
#region SetFocus
        [Fact]
        public void When_SetFocus_With_Hwnd_Then_Call_SetForegroundWindow()
        {
            // Arrange
            var hWnd = new IntPtr(2);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out Mock<IWindowTracker> windowTracker, pinvokeHandler: out Mock<IPInvokeHandler> pInvokeHandler, hWnd);
            windowTracker.Setup(m => m.RevalidateHwnd(sut, hWnd)).Returns(true);
            
            // Act
            sut.SetFocus();

            // Assert
            pInvokeHandler.Verify(m => m.SetForegroundWindow(hWnd));
        }
        
        [Fact]
        public void When_SetFocus_Then_Notify()
        {
            // Arrange
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out _);
            var wantFocusTriggered = false;
            sut.WantFocus += (sender, args) => wantFocusTriggered = true;

            // Act
            sut.SetFocus();

            // Assert
            wantFocusTriggered.Should().BeTrue();
        }
#endregion
#region QuitNode
        [Fact]
        public void When_QuitNode_With_Hwnd_Then_Post_Quit_Message()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pInvokeHandler, hWnd);

            // Act
            sut.QuitNodeImpl().Wait();

            // Assert
            pInvokeHandler.Verify(m => m.SendMessage(hWnd, PInvoker.WM_CLOSE, IntPtr.Zero, IntPtr.Zero));
        }
#endregion
#region RemoveChild
        [Fact]
        public void When_RemoveChild_Then_Return_False()
        {
            // Arrange
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out _);

            // Act
            var result = sut.RemoveChild(null);

            // Assert
            result.Should().BeFalse();
        }
#endregion
#region SetFullscreenRect
        [Fact]
        public void When_SetFullscreenRect_And_StyleIsFullscreen_Then_SetWindowPos()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pInvokeHandler, hWnd);
            sut.Style = NodeStyle.FullscreenOne;

            // Act
            sut.SetFullscreenRect(expectedRect);

            // Assert
            pInvokeHandler.Verify(m => m.SetWindowPos(hWnd, It.IsAny<IntPtr>(), expectedRect.Left, expectedRect.Top, expectedRect.Right - expectedRect.Left, expectedRect.Bottom - expectedRect.Top, It.IsAny<SetWindowPosFlags>()));
        }

        [Fact]
        public void When_SetFullscreenRect_And_Style_Is_NOT_Fullscree3n_Then_Do_Not_Call_SetWindowPos()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pInvokeHandler, hWnd);
            sut.Style = NodeStyle.Tile;
            pInvokeHandler.Setup(m => 
            m.SetWindowPos(It.IsAny<IntPtr>(), It.IsAny<IntPtr>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SetWindowPosFlags>()))
            .Throws(new Exception("SetWindowPos should not be called when nodes style is != Fullscreen and SetFullscreenRect is called"));

            // Act & Assert
            sut.SetFullscreenRect(expectedRect);
        }
#endregion
#region UpdateRect
        [Fact]
        public void When_UpdateRect_Then_UpdateRect()
        {
            // Arrange
            var expectedRect = new RECT(100, 200, 300, 400);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out _);

            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeTrue();
            sut.Rect.Should().Equals(expectedRect);
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_StyleIsTile_Then_SetWindowPos()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            sut.Style = NodeStyle.Tile;
            
            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeTrue();
            pinvokeHandler.Verify(m => m.SetWindowPos(hWnd, It.IsAny<IntPtr>(), expectedRect.Left, expectedRect.Top, expectedRect.Right - expectedRect.Left, expectedRect.Bottom - expectedRect.Top, It.IsAny<SetWindowPosFlags>()));
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_StyleIsFloating_Then_SetWindowPos()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            sut.Style = NodeStyle.Floating;
            
            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeTrue();
            pinvokeHandler.Verify(m => m.SetWindowPos(hWnd, It.IsAny<IntPtr>(), expectedRect.Left, expectedRect.Top, expectedRect.Right - expectedRect.Left, expectedRect.Bottom - expectedRect.Top, It.IsAny<SetWindowPosFlags>()));
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_GetWindowRectIsFalse_Then_ReturnTrue()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var butWasRect = new RECT(100, 200, 200, 300);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            pinvokeHandler.Setup(m => m.GetWindowRect(hWnd, out butWasRect)).Returns(false);

            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_RectDifference_But_IsLessThanUpdateRect_Then_ReturnTrue()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var butWasRect = new RECT(100, 200, 200, 300);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            pinvokeHandler.Setup(m => m.GetWindowRect(hWnd, out butWasRect)).Returns(true);

            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_RectDifference_And_IsBiggerThanRequested_Then_UpdateRectWithNewRect_And_ReturnFalse()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var butWasRect = new RECT(100, 200, 400, 500);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            pinvokeHandler.Setup(m => m.GetWindowRect(hWnd, out butWasRect)).Returns(true);

            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeFalse();
            sut.FixedRect.Should().BeTrue();
            sut.Rect.Should().Equals(butWasRect);
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_RectDifference_And_IsBiggerThanRequested_Then_UpdateRectWithNewRect()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var butWasRect = new RECT(100, 200, 400, 500);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            pinvokeHandler.Setup(m => m.GetWindowRect(hWnd, out butWasRect)).Returns(true);

            // Act
            sut.UpdateRect(expectedRect);

            // Assert
            sut.Rect.Should().Equals(butWasRect);
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_RectDifference_And_IsBiggerThanRequested_Then_SetFixedRectTrue()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var butWasRect = new RECT(100, 200, 400, 500);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            pinvokeHandler.Setup(m => m.GetWindowRect(hWnd, out butWasRect)).Returns(true);

            // Act
            sut.UpdateRect(expectedRect);

            // Assert
            sut.FixedRect.Should().BeTrue();
        }

        [Fact]
        public void When_UpdateRect_And_HwndIsSet_And_RectDifference_And_IsBiggerThanRequested_Then_ReturnFalse()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var expectedRect = new RECT(100, 200, 300, 400);
            var butWasRect = new RECT(100, 200, 400, 500);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler, hwnd: hWnd);
            pinvokeHandler.Setup(m => m.GetWindowRect(hWnd, out butWasRect)).Returns(true);

            // Act
            var result = sut.UpdateRect(expectedRect);

            // Assert
            result.Should().BeFalse();
        }
#endregion
#region Changedirection
        [Fact]
        public void When_ChangeDirection_Then_SetOwnsDirection()
        {
            // Arrange
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out _, direction: Direction.Horizontal);

            // Act
            sut.ChangeDirection(Direction.Vertical);

            // Assert
            sut.Direction.Should().Be(Direction.Vertical);
        }

#endregion
#region Dispose
        [Fact]
        public void When_Disposed_And_HwndIsSet_Then_RemoveWindowFromWindowTracker()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out Mock<IWindowTracker> windowTracker, pinvokeHandler: out _, hwnd: hWnd);

            // Act
            sut.Dispose();

            // Assert
            windowTracker.Verify(m => m.removeWindow(hWnd));
        }

        [Fact]
        public void When_Disposed_Then_ClearHwnd()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out _, hwnd: hWnd);

            // Act
            sut.Dispose();

            // Assert
            sut.Hwnd.Should().Equals(IntPtr.Zero);
        }

        [Fact]
        public void When_Disposed_And_HwndIsSet_Then_UnsubscribeFromFocusListener()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var guid = Guid.NewGuid();
            var focusHandler = new Mock<IFocusHandler>();

            focusHandler.Setup(m => m.AddListener(hWnd, It.IsAny<Action<bool>>())).Returns(guid);
            var sut = CreateSut(focusHandler: focusHandler, windowHandler: out _, windowTracker: out Mock<IWindowTracker> windowTracker, pinvokeHandler: out _, hwnd: hWnd);
            
            // Act
            sut.Dispose();

            // Assert
            focusHandler.Verify(m => m.RemoveListener(hWnd, guid));
        }

        [Fact]
        public void When_Disposed_And_HwndIsSet_Then_UnsubscribeFromDragHandlerEvents()
        {
            // Arrange
            var hWnd = new IntPtr(1);
            var guid = Guid.NewGuid();
            var sut = CreateSut(dragHandler: out Mock<IDragHandler> dragHandler, hwnd: hWnd);
            dragHandler.SetupRemove(m => m.OnDragEnd -= (sender, arg) => {});
            dragHandler.SetupRemove(m => m.OnDragStart -= (sender, arg) => {});
            dragHandler.SetupRemove(m => m.OnDragMove -= (sender, arg) => {});

            // Act
            sut.Dispose();
            
            // Assert
            dragHandler.VerifyRemove(m => m.OnDragEnd -= It.IsAny<EventHandler<DragEndEvent>>());
            dragHandler.VerifyRemove(m => m.OnDragStart -= It.IsAny<EventHandler<DragStartEvent>>());
            dragHandler.VerifyRemove(m => m.OnDragMove -= It.IsAny<EventHandler<DragMoveEvent>>());
        }

        [Fact]
        public void When_Disposed_Then_NotifyDelete()
        {
            // Arrange
            bool deletedCalled = false;
            object recSender = null;
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: out _);
            sut.Deleted += (sender, arg) => {
                recSender = sender;;
                deletedCalled = true;
            };

            // Act
            sut.Dispose();

            // Assert
            deletedCalled.Should().BeTrue();
            recSender.Should().BeEquivalentTo(sut);
        }

#endregion
#region Restore
        [Fact]
        public void When_Restore_Then_RestoreWindowState()
        {
            // Arrange
            var styleRet = new IntPtr(1);
            var styleExRet = new IntPtr(2);
            var hwnd = new IntPtr(3);
            var pinvokeHandler = new Mock<IPInvokeHandler>();
            var rect = new RECT(10, 20, 30, 40);
            pinvokeHandler.Setup(m => m.GetWindowLongPtr(hwnd, GWL_STYLE)).Returns(styleRet);
            pinvokeHandler.Setup(m => m.GetWindowLongPtr(hwnd, GWL_EXSTYLE)).Returns(styleExRet);
            pinvokeHandler.Setup(m => m.GetWindowRect(hwnd, out rect));
            var sut = CreateSut(focusHandler: out _, windowHandler: out _, windowTracker: out _, pinvokeHandler: pinvokeHandler, hwnd: hwnd);

            // Act
            sut.Restore();

            // Assert
            pinvokeHandler.Verify(m => m.SetWindowLongPtr(It.Is<HandleRef>(h => h.Handle == hwnd), GWL_STYLE, styleRet));
            pinvokeHandler.Verify(m => m.SetWindowLongPtr(It.Is<HandleRef>(h => h.Handle == hwnd), GWL_EXSTYLE, styleExRet));
            pinvokeHandler.Verify(m => m.SetWindowPos(hwnd, HWND_NOTOPMOST, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, SetWindowPosFlags.SWP_SHOWWINDOW)); // Bug #87, restore HWND_NOTOPMOST
        }
#endregion
#region HandleOnDragStart
        [Fact]
        public void When_OnDragStart_And_StyleIsNotFloating_And_HwndEquals_Then_ChangeStyleToFloating()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;

            // Act
            dragHandler.Raise(m => m.OnDragStart += null, null, new DragStartEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Floating);
        }
        [Fact]
        public void When_OnDragStart_And_StyleIsNotFloating_And_HwndDoNotEquals_Then_DoNothing()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;

            // Act
            dragHandler.Raise(m => m.OnDragStart += null, null, new DragStartEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt32() + 1 }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Tile);
        }
#endregion
#region HandleOnDragMove
        [Fact]
        public void When_OnDragMove_Then_GetWindowRect_And_UpdateOwnRectWithIt()
        {
            // Arranges
            var expected = new RECT(10, 20, 100, 200);
            var hwnd = new IntPtr(1);
            var sut = CreateSut(dragHandler: out Mock<IDragHandler> dragHandler, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler,  hwnd: hwnd);
            dragHandler.Raise(m => m.OnDragStart += null, null, new DragStartEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            pinvokeHandler.Setup(m => m.GetWindowRect(hwnd, out expected)).Returns(true);

            // Act
            dragHandler.Raise(m => m.OnDragMove += null, null, new DragMoveEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            
            // Assert
            pinvokeHandler.Verify(m => m.GetWindowRect(hwnd, out expected));
            sut.Rect.Should().Be(expected);
        }

        [Fact]
        public void When_OnDragMove_And_GetWindowRectFails_Then_UpdateOwnRectWithParameterCoordinates()
        {
            // Arranges
            var notExpected = new RECT(10, 20, 100, 200);
            var hwnd = new IntPtr(1);
            var sut = CreateSut(dragHandler: out Mock<IDragHandler> dragHandler, pinvokeHandler: out Mock<IPInvokeHandler> pinvokeHandler,  hwnd: hwnd);
            dragHandler.Raise(m => m.OnDragStart += null, null, new DragStartEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            pinvokeHandler.Setup(m => m.GetWindowRect(hwnd, out notExpected)).Returns(false);

            // Act
            dragHandler.Raise(m => m.OnDragMove += null, null, new DragMoveEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64(), lParam = 0x00080003 })); // Note: Upper dword: y coordinate, lower dword: x coordinates
            
            // Assert
            pinvokeHandler.Verify(m => m.GetWindowRect(hwnd, out notExpected));
            sut.Rect.Left.Should().Be(3);
            sut.Rect.Top.Should().Be(8);
        }

        [Fact]
        public void When_OnDragMove_And_StyleIsNotFloating_And_HwndEquals_Then_ChangeStyleToFloating()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;
            dragHandler.Raise(m => m.OnDragStart += null, null, new DragStartEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            sut.Style = NodeStyle.Tile;

            // Act
            sut.Style.Should().Be(NodeStyle.Tile);
            dragHandler.Raise(m => m.OnDragMove += null, null, new DragMoveEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Floating);
        }

        [Fact]
        public void When_OnDragMove_And_StyleIsNotFloating_And_HwndDoNotEquals_Then_DoNothing()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;
            dragHandler.Raise(m => m.OnDragStart += null, null, new DragStartEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            sut.Style = NodeStyle.Tile;

            // Act
            sut.Style.Should().Be(NodeStyle.Tile);
            dragHandler.Raise(m => m.OnDragMove += null, null, new DragMoveEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() + 1 }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Tile);
        }

        [Fact]
        public void When_OnDragMove_And_OnDragStartHaveNotBeenTriggered_Then_DoNothing()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;

            // Act
            dragHandler.Raise(m => m.OnDragMove += null, null, new DragMoveEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Tile);
        }
#endregion
#region HandleOnDragEnd
        [Fact]
        public void When_OnDragEnd_And_StyleIsNotFloating_And_HwndEquals_Then_ChangeStyleToFloating()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;

            // Act
            dragHandler.Raise(m => m.OnDragEnd += null, null, new DragEndEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt64() }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Floating);
        }
        [Fact]
        public void When_OnDragEnd_And_StyleIsNotFloating_And_HwndDoNotEquals_Then_DoNothing()
        {
            // Arranges
            var hwnd = new IntPtr(1);
            var sut = CreateSut(out Mock<IDragHandler> dragHandler, hwnd: hwnd);
            sut.Style = NodeStyle.Tile;

            // Act
            dragHandler.Raise(m => m.OnDragEnd += null, null, new DragEndEvent(new PipeMessageEx { wParam = (ulong)hwnd.ToInt32() + 1 }));
            
            // Assert
            sut.Style.Should().Be(NodeStyle.Tile);
        }
#endregion

#region Helpers
        private WindowNode CreateSut(out Mock<IDragHandler> dragHandler, IntPtr? hwnd = null, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            var hWnd = hwnd ?? IntPtr.Zero;
            dragHandler = new Mock<IDragHandler>();
            var focusHandler = new Mock<IFocusHandler>();
            var windowHandler = new Mock<IWindowEventHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            var windowTracker = new Mock<IWindowTracker>();
            var pinvokeHandler = new Mock<IPInvokeHandler>();

            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);
            return new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, rect ?? new RECT(10, 20, 30, 40), hWnd, direction, parent);
        }

        private WindowNode CreateSut(out Mock<IDragHandler> dragHandler, out Mock<IPInvokeHandler> pinvokeHandler, IntPtr? hwnd = null, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            var hWnd = hwnd ?? IntPtr.Zero;
            dragHandler = new Mock<IDragHandler>();
            var focusHandler = new Mock<IFocusHandler>();
            var windowHandler = new Mock<IWindowEventHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            var windowTracker = new Mock<IWindowTracker>();
            pinvokeHandler = new Mock<IPInvokeHandler>();

            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);
            return new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, rect ?? new RECT(10, 20, 30, 40), hWnd, direction, parent);
        }

        private WindowNode CreateSut(out Mock<IFocusHandler> focusHandler, out Mock<IWindowEventHandler> windowHandler, out Mock<IWindowTracker> windowTracker, out Mock<IPInvokeHandler> pinvokeHandler, IntPtr? hwnd = null, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            focusHandler = new Mock<IFocusHandler>();
            return CreateSut(focusHandler, out windowHandler, out windowTracker, out pinvokeHandler, hwnd, rect, parent, direction);
        }

        private WindowNode CreateSut(Mock<IFocusHandler> focusHandler, out Mock<IWindowEventHandler> windowHandler, out Mock<IWindowTracker> windowTracker, out Mock<IPInvokeHandler> pinvokeHandler, IntPtr? hwnd = null, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            var hWnd = hwnd ?? IntPtr.Zero;
            var dragHandler = new Mock<IDragHandler>();
            windowHandler = new Mock<IWindowEventHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            windowTracker = new Mock<IWindowTracker>();
            pinvokeHandler = new Mock<IPInvokeHandler>();

            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);

            return new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, rect ?? new RECT(10, 20, 30, 40), hWnd, direction, parent);
        }

        private WindowNode CreateSut(out Mock<IFocusHandler> focusHandler, out Mock<IWindowEventHandler> windowHandler, out Mock<IWindowTracker> windowTracker, Mock<IPInvokeHandler> pinvokeHandler, IntPtr? hwnd = null, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            return CreateSut(dragHandler: out _, focusHandler: out focusHandler, windowHandler: out windowHandler, windowTracker: out windowTracker, pinvokeHandler: pinvokeHandler, hwnd: hwnd, rect: rect, parent: parent, direction: direction);
        }

        private WindowNode CreateSut(out Mock<IDragHandler> dragHandler, out Mock<IFocusHandler> focusHandler, out Mock<IWindowEventHandler> windowHandler, out Mock<IWindowTracker> windowTracker, Mock<IPInvokeHandler> pinvokeHandler, IntPtr? hwnd = null, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            var hWnd = hwnd ?? IntPtr.Zero;
            dragHandler = new Mock<IDragHandler>();
            focusHandler = new Mock<IFocusHandler>();
            var signalHandler = new Mock<ISignalHandler>();
            windowHandler = new Mock<IWindowEventHandler>();
            windowTracker = new Mock<IWindowTracker>();

            pinvokeHandler.Setup(m => m.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);
            return new WindowNode(dragHandler.Object, focusHandler.Object, signalHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, rect ?? new RECT(10, 20, 30, 40), hWnd, direction, parent);
        }
#endregion
    }
}