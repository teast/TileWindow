using System;
using TileWindow.Dto;

namespace TileWindow.Handlers
{
    /// <summary>
    /// Describes an interface for handling messages
    /// </summary>
    public interface IHandler: IDisposable
    {
        /// <summary>
        /// Handle an incomming message
        /// </summary>
        /// <param name="msg">the incoming message</param>
        void HandleMessage(PipeMessage msg);

        /// <summary>
        /// Will be called right after creation of object
        /// </summary>
        /// <param name="config">Applications main config</param>
        void ReadConfig(AppConfig config);

        /// <summary>
        /// Will be called right before main message loop, for some last time initialize
        /// </summary>
        void Init();

        /// <summary>
        /// Will be called when the handler should quit
        /// </summary>
        void Quit();
    }
}