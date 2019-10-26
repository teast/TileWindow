using System;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Moq;
using TileWindow.Handlers;
using TileWindow.Nodes;
using TileWindow.Nodes.Creaters;
using TileWindow.Nodes.Renderers;
using TileWindow.Trackers;

namespace TileWindow.Tests.TestHelpers
{
    public static class NodeHelper
    {
        public static Delegate GetDelegate(this Node node, string eventName)
        {
            return typeof(Node).GetField(eventName, BindingFlags.NonPublic |BindingFlags.Instance).GetValue(node) as Delegate;
        }

        public static void EventShouldNotBeNull(this Node node,string eventName)
        {
            var dg = node.GetDelegate(eventName);
            dg.Should().NotBeNull();
        }

        public static void EventShouldBeNull(this Node node,string eventName)
        {
            var dg = node.GetDelegate(eventName);
            dg.Should().BeNull();
        }

        public static void TestTakeOver(this Node node, Node parent)
        {
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node) as Delegate;

            styleBound.Should().NotBeNull();
            requestBound.Should().NotBeNull();
            node.Parent.Should().BeEquivalentTo(parent);
            node.Depth.Should().Be(parent.Depth + 1);
        }

        public static void TestDisconnect(this Node node, Node parent)
        {
            var styleBound = typeof(Node).GetField(nameof(Node.StyleChanged), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node) as Delegate;
            var requestBound = typeof(Node).GetField(nameof(Node.RequestRectChange), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node) as Delegate;

            styleBound.Should().BeNull();
            requestBound.Should().BeNull();
            node.Parent.Should().NotBeEquivalentTo(parent);
        }

        public static Mock<Node> CreateMockNode(RECT? rect = null, Direction direction = Direction.Horizontal, Node parent = null)
        {
            return new Mock<Node>(rect ?? new RECT(), direction, parent) { CallBase = true };
        }

        public static Mock<IVirtualDesktop> CreateMockDesktop()
        {
            return new Mock<IVirtualDesktop>();
        }

        public static Mock<ScreenNode> CreateMockScreen(RECT? rect = null, Direction direction = Direction.Horizontal, bool callPostInit = true, params Node[] childs)
        {
            var renderer = new Mock<IRenderer>();
            var createContainer = new Mock<IContainerNodeCreater>();
            var windowTracker = new Mock<IWindowTracker>();
            var screen = new Mock<ScreenNode>(
                "Screen",
                renderer.Object,
                createContainer.Object,
                windowTracker.Object,
                rect ?? new RECT(10, 10, 15, 15),
                direction
            ) { CallBase = true };
            if (callPostInit)
                screen.Object.PostInit(childs);
            return screen;
        }

        public static Mock<ContainerNode> CreateMockContainer(RECT? rect = null, Direction direction = Direction.Horizontal, Node parent = null, bool callPostInit = true, params Node[] childs)
        {
            var renderer = new Mock<IRenderer>();
            var createContainer = new Mock<IContainerNodeCreater>();
            var windowTracker = new Mock<IWindowTracker>();
            var mockContainer = new Mock<ContainerNode>(
                renderer.Object,
                createContainer.Object,
                windowTracker.Object,
                rect == null ? new RECT(10, 10, 15, 15) : rect,
                direction,
                parent
            ) { CallBase = true };
            if (callPostInit)
                mockContainer.Object.PostInit(childs);
            return mockContainer;
        }

       public static WindowNode CreateWindowNode(IntPtr? hWnd = null)
        {
            var hwnd = hWnd ?? IntPtr.Zero;
            var focusHandler = new Mock<IFocusHandler>();
            var windowHandler = new Mock<IWindowEventHandler>();
            var windowTracker = new Mock<IWindowTracker>();
            var pinvokeHandler = new Mock<IPInvokeHandler>();
            pinvokeHandler.Setup(m => m.GetClassName(hwnd, It.IsAny<StringBuilder>(), It.IsAny<int>())).Returns(1);
            pinvokeHandler.Setup(m => m.GetWindowLongPtr(hwnd, It.IsAny<int>())).Returns(new IntPtr(PInvoker.WS_CAPTION | PInvoker.WS_SIZEBOX));
            return new WindowNode(focusHandler.Object, windowHandler.Object, windowTracker.Object, pinvokeHandler.Object, new RECT(), hwnd);
        }

        public static ContainerNode CreateContainerNode(out Mock<IRenderer> renderer, out Mock<IContainerNodeCreater> containerCreater, out Mock<IWindowTracker> windowTracker, RECT? rect = null, Node parent = null, Direction direction = Direction.Horizontal)
        {
            renderer = new Mock<IRenderer>();
            containerCreater = new Mock<IContainerNodeCreater>();
            windowTracker = new Mock<IWindowTracker>();

            var node = new ContainerNode(
                renderer.Object,
                containerCreater.Object,
                windowTracker.Object,
                rect ?? new RECT(10, 20, 30, 40),
                direction,
                parent);
            return node;
        }
    }
}