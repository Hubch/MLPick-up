// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLPickup.Common.Concurrency
{
    using System;

    public interface IScheduledRunnable : IRunnable, IScheduledTask, IComparable<IScheduledRunnable>
    {
    }
}