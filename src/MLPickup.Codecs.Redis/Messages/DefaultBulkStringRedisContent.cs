// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLModeling.Codecs.Redis.Messages
{
    using System.Text;
    using MLModeling.Buffers;
    using MLModeling.Common.Utilities;

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