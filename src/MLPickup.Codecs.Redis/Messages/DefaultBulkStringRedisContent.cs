﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLPickup.Codecs.Redis.Messages
{
    using System.Text;
    using MLPickup.Buffers;
    using MLPickup.Common.Utilities;

    public class DefaultBulkStringRedisContent : DefaultByteBufferHolder, IBulkStringRedisContent
    {
        public DefaultBulkStringRedisContent(IByteBuffer buffer)
            : base(buffer)
        {
        }

        public override IByteBufferHolder Replace(IByteBuffer content) => new DefaultBulkStringRedisContent(content);

        public override string ToString() =>
            new StringBuilder(StringUtil.SimpleClassName(this))
                .Append('[')
                .Append("content=")
                .Append(this.Content)
                .Append(']')
                .ToString();
    }
}