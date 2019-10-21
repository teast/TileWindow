using System.Collections.Generic;

namespace TileWindow.Nodes.Renderers
{
    /// <summary>
    /// An <see cref="IRenderer" /> is responsible for layout handling for child nodes of given owner
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Prepare data for a new set of childs or owner.
        /// </summary>
        /// <param name="owner">Owner node</param>
        /// <param name="childs">Childs to the owner</param>
        /// <remarks>This will be called when the collection of childs changes and/or owner changes</remarks>
        void PreUpdate(Node owner, List<Node> childs);

        /// <summary>
        /// Do the actual layout structure on childs from <see cref="PreUpdate" /> call
        /// </summary>
        /// <param name="ignoreChildsWithIndex">list of index to not handle</param>
        /// <returns>if result is false then newRect will contain new wanted size for owner</returns>
        (bool result, RECT newRect) Update(List<int> ignoreChildsWithIndex);
    } 
}