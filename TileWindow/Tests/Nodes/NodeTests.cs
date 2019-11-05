using System;
using TileWindow.Nodes;
using Xunit;
using FluentAssertions;
using TileWindow.Trackers;

namespace TileWindow.Tests.Nodes
{
    public class NodeTests
    {
        private static RECT defaultRect = new RECT(10, 20, 30, 40);

        private class Tester : Node
        {
            public Tester()
            : base(defaultRect, Direction.Horizontal, null)
            {
            }

            public override NodeTypes WhatType => throw new NotImplementedException();

            public override void PostInit()
            {
                throw new NotImplementedException();
            }

            public override Node AddWindow(IntPtr hWnd, ValidateHwndParams validation = null)
            {
                throw new NotImplementedException();
            }

            public override bool AddNodes(params Node[] nodes)
            {
                throw new NotImplementedException();
            }
            
            public override bool ReplaceNode(Node node, Node newNode)
            {
                throw new NotImplementedException();
            }

            public override bool Hide()
            {
                throw new NotImplementedException();
            }
            public override Node FindNodeWithId(long id)
            {
                throw new NotImplementedException();
            }

            public override bool Show()
            {
                throw new NotImplementedException();
            }
            public override bool Restore()
            {
                throw new NotImplementedException();
            }

            public override bool RemoveChild(Node child)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void When_Resize_Then_Trigger_RequestRectChange_Event()
        {
            // Assign
            var sut = new Tester();
            var called = false;
            sut.RequestRectChange += (from, arg) => called = true;

            // Act
            sut.Resize(10, TransferDirection.Left);

            // Assert
            called.Should().BeTrue();
        }

        [Fact]
        public void When_Resize_Left_Then_Resize_Correct()
        {
            // Assign
            var newRect = new RECT(20, 30, 40, 50);
            var sut = new Tester();
            Node sender = null;
            RequestRectChangeEventArg args = null;
            sut.RequestRectChange += (from, arg) => {
                sender = from as Node;
                args = arg;
            };

            // Act
            sut.Resize(10, TransferDirection.Left);

            // Assert
            args.OldRect.Should().Equals(defaultRect);
            args.NewRect.Right.Should().Equals(defaultRect.Right + 10);
        }

        [Fact]
        public void When_Resize_Up_Then_Resize_Correct()
        {
            // Assign
            var newRect = new RECT(20, 30, 40, 50);
            var sut = new Tester();
            Node sender = null;
            RequestRectChangeEventArg args = null;
            sut.RequestRectChange += (from, arg) => {
                sender = from as Node;
                args = arg;
            };

            // Act
            sut.Resize(10, TransferDirection.Up);

            // Assert
            args.OldRect.Should().Equals(defaultRect);
            args.NewRect.Right.Should().Equals(defaultRect.Right + 10);
        }

        [Fact]
        public void When_Resize_Right_Then_Resize_Correct()
        {
            // Assign
            var newRect = new RECT(20, 30, 40, 50);
            var sut = new Tester();
            Node sender = null;
            RequestRectChangeEventArg args = null;
            sut.RequestRectChange += (from, arg) => {
                sender = from as Node;
                args = arg;
            };

            // Act
            sut.Resize(10, TransferDirection.Right);

            // Assert
            args.OldRect.Should().Equals(defaultRect);
            args.NewRect.Right.Should().Equals(defaultRect.Right + 10);
        }

        [Fact]
        public void When_Resize_Down_Then_Resize_Correct()
        {
            // Assign
            var newRect = new RECT(20, 30, 40, 50);
            var sut = new Tester();
            Node sender = null;
            RequestRectChangeEventArg args = null;
            sut.RequestRectChange += (from, arg) => {
                sender = from as Node;
                args = arg;
            };

            // Act
            sut.Resize(10, TransferDirection.Down);

            // Assert
            args.OldRect.Should().Equals(defaultRect);
            args.NewRect.Right.Should().Equals(defaultRect.Right + 10);
        }
    }
}
