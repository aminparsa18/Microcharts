// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Plugin.Maui.Microchart.Abstracts;

namespace Plugin.Maui.Microchart
{
    using System;

    /// <summary>
    /// A loop that executes an action.
    /// </summary>
    public static class Timer
    {
        /// <summary>
        /// Gets or sets a factory used to instanciate timers.
        /// </summary>
        /// <value>The factory function.</value>
        public static Func<Abstracts.ITimer> Create { get; set; } = () => new DelayTimer();
    }
}
