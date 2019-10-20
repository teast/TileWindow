using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using TileWindow.Dto;
using TileWindow.Handlers;
using TileWindow.Handlers.I3wm;
using TileWindow.Handlers.I3wm.Nodes;
using TileWindow.Handlers.I3wm.Nodes.Creaters;
using TileWindow.Tests.TestHelpers;
using Xunit;

namespace TileWindow.Tests.Handlers.I3wm
{
    public class StartupHandlerTests
    {
        [Fact]
        public void When_DisplayChange_Then_CallScreenChanged()
        {
            // Arrange
            uint signalId = 666;
            (var screens, var screenList) = SetupScreens(1);
            (var collection, var desktops) = SetupDeskops(1);
            var sut = CreateSut(screens: screens, desktopCollection: collection, signal: out Mock<ISignalHandler> signal);
            var msg = new PipeMessage { msg = signalId };
            
            signal.SetupGet(m => m.WMC_DISPLAYCHANGE).Returns(signalId);
            screenList[0].SetupGet(m => m.WorkingArea).Returns(new RECT(0, 0, 20, 20));
            var expected = screens.Object.AllScreens.GetOrderRect().rect.ToArray();

            // Act
            sut.HandleMessage(msg);

            // Assert
            desktops[0].Verify(m => m.ScreensChanged(expected, Direction.Vertical));
        }

        [Fact]
        public void When_DisplayChange_Then_SignalRestartThread()
        {
            // Arrange
            uint signalId = 666;
            (var screens, var screenList) = SetupScreens(1);
            (var collection, var desktops) = SetupDeskops(1);
            var sut = CreateSut(screens: screens, desktopCollection: collection, signal: out Mock<ISignalHandler> signal);
            var msg = new PipeMessage { msg = signalId };
            var restartThreadsCalled = false;

            signal.SetupGet(m => m.WMC_DISPLAYCHANGE).Returns(signalId);
            screenList[0].SetupGet(m => m.WorkingArea).Returns(new RECT(0, 0, 20, 20));
            Startup.ParserSignal.RestartThreads += (sender, args) => restartThreadsCalled = true;

            // Act
            sut.HandleMessage(msg);

            // Assert
            restartThreadsCalled.Should().BeTrue();
        }

#region Helpers
        private (Mock<IScreens>, List<Mock<IScreenInfo>>) SetupScreens(int nrOfScreens)
        {
            var screens = new List<Mock<IScreenInfo>>();
            var result = new Mock<IScreens>();

            for(var i = 0; i < nrOfScreens; i++)
            {
                var screen = new Mock<IScreenInfo>();
                screen.SetupGet(m => m.Primary).Returns(true);
                screen.SetupGet(m => m.WorkingArea).Returns(new RECT(0, 0, 10, 10));
                screens.Add(screen);
            }

            result.SetupGet(m => m.AllScreens).Returns(screens.Select(m => m.Object));
            return (result, screens);
        }

        private (Mock<IVirtualDesktopCollection>, List<Mock<IVirtualDesktop>>) SetupDeskops(int nrOfDesktops)
        {
            var desktops = new List<Mock<IVirtualDesktop>>();
            var result = new Mock<IVirtualDesktopCollection>();

            for(var i = 0; i < nrOfDesktops; i++)
            {
                desktops.Add(NodeHelper.CreateMockDesktop());
                result.Setup(m => m[i]).Returns(desktops[i].Object);
            }

            result.SetupGet(m => m.ActiveDesktop).Returns(() => desktops[result.Object.Index].Object);
            result.Setup(m => m.GetEnumerator()).Returns(desktops.Select(m => m.Object).GetEnumerator());
            result.SetupGet(m => m.Count).Returns(() => desktops.Count);
            return (result, desktops);
        }

        private StartupHandler CreateSut(Mock<IScreens> screens, Mock<IVirtualDesktopCollection> desktopCollection, out Mock<ISignalHandler> signal)
        {
            return CreateSut(screens, desktopCollection, out _, out _, out _, out signal, out _, out _, out _, out _);
        }
        private StartupHandler CreateSut()
        {
            return CreateSut(out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        }

        private StartupHandler CreateSut(out Mock<IScreens> screens,
            out Mock<IVirtualDesktopCollection> desktopCollection,
            out Mock<IContainerNodeCreater> containerCreator,
            out Mock<IVirtualDesktopCreater> desktopCreater,
            out Mock<IScreenNodeCreater> screenCreater,
            out Mock<ISignalHandler> signal,
            out Mock<IKeyHandler> keyHandler,
            out Mock<ICommandHelper> commandHelper,
            out Mock<IWindowTracker> windowTracker,
            out Mock<IPInvokeHandler> pinvokeHandler)
        {
            screens = new Mock<IScreens>();
            desktopCollection = new Mock<IVirtualDesktopCollection>();
            return CreateSut(screens, desktopCollection,
                    out containerCreator,
                    out desktopCreater,
                    out screenCreater,
                    out signal,
                    out keyHandler,
                    out commandHelper,
                    out windowTracker,
                    out pinvokeHandler);
        }

        private StartupHandler CreateSut(Mock<IScreens> screens,
            Mock<IVirtualDesktopCollection> desktopCollection,
            out Mock<IContainerNodeCreater> containerCreator,
            out Mock<IVirtualDesktopCreater> desktopCreater,
            out Mock<IScreenNodeCreater> screenCreater,
            out Mock<ISignalHandler> signal,
            out Mock<IKeyHandler> keyHandler,
            out Mock<ICommandHelper> commandHelper,
            out Mock<IWindowTracker> windowTracker,
            out Mock<IPInvokeHandler> pinvokeHandler)
        {
            containerCreator = new Mock<IContainerNodeCreater>();
            desktopCreater = new Mock<IVirtualDesktopCreater>();
            screenCreater = new Mock<IScreenNodeCreater>();
            signal = new Mock<ISignalHandler>();
            keyHandler = new Mock<IKeyHandler>();
            commandHelper = new Mock<ICommandHelper>();
            windowTracker = new Mock<IWindowTracker>();
            pinvokeHandler = new Mock<IPInvokeHandler>();

            return new StartupHandler(screens.Object,
                        desktopCollection.Object,
                        containerCreator.Object,
                        desktopCreater.Object,
                        screenCreater.Object,
                        signal.Object,
                        keyHandler.Object,
                        commandHelper.Object,
                        windowTracker.Object,
                        pinvokeHandler.Object);
        }
#endregion
    }
}