﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLModeling.Common.Internal
{
    public interface IPlatform
    {
        int GetCurrentProcessId();

        byte[] GetDefaultDeviceId();
    }
}