using System.Collections.ObjectModel;
using FluentAssertions;
using TileWindow.Nodes.Renderers;
using TileWindow.Tests.TestHelpers;
using Xunit;

namespace TileWindow.Tests.Nodes.Renderers
{
    public class TileRendererTests
    {
        [Fact]
        public void When_PreUpdate_And_ChildsIsEmpty_Then_SetAllocatableToWidthHeight()
        {
            // Arrange
            var rect = new RECT(0, 0, 10 ,15);
            var expectedWidth = 10;
            var expectedHeight = 15;
            var owner = NodeHelper.CreateMockContainer(rect: rect);
            var sut = CreateSut();

            // Act
            sut.PreUpdate(owner.Object, new Collection<TileWindow.Nodes.Node>());

            // Assert
            sut.AllocatableHeight.Should().Be(expectedHeight);
            sut.AllocatableWidth.Should().Be(expectedWidth);
        }

#region Helpers
        private TileRenderer CreateSut()
        {
            return new TileRenderer();
        }
#endregion
    }
}