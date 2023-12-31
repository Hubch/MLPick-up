﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLPickup.Common.Internal
{
    using MLPickup.Common.Utilities;

    public interface IAppendable
    {
        IAppendable Append(char c);

        IAppendable Append(ICharSequence sequence);

        IAppendable Append(ICharSequence sequence, int start, int end);
    }
}
