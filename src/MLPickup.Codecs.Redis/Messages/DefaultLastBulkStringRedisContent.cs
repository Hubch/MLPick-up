﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLPickup.Codecs.Redis.Messages
{
    using MLPickup.Buffers;

    public sealed class DefaultLastBulkStringRedisContent : DefaultBulkStringRedisContent, ILastBulkStringRedisContent
    {
        public DefaultLastBulkStringRedisContent(IByteBuffer content)
            : base(content)
        {
        }

        public override IByteBufferHolder Replace(IByteBuffer buffer) => new DefaultLastBulkStringRedisContent(buffer);
    }
}