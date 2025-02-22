// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace CommunityToolkit.WinUI.UI.Controls
{
    /// <summary>
    /// Internal class used to provide helpers for controls
    /// </summary>
    internal static partial class ControlHelpers
    {
        /// <summary>
        /// Get the visual associated with an UIElement
        /// </summary>
        /// <param name="element">Source UIElement</param>
        /// <returns>ContainerVisual associated with the element</returns>
        public static ContainerVisual GetVisual(this UIElement element)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            var root = hostVisual.Compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(element, root);
            return root;
        }
    }
}