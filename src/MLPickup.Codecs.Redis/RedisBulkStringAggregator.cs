﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLModeling.Codecs.Redis
{
    using System;
    using MLModeling.Codecs.Redis.Messages;

    public sealed class RedisBulkStringAggregator : MessageAggregator<IRedisMessage, BulkStringHeaderRedisMessage,
        IBulkStringRedisContent, IFullBulkStringRedisMessage>
    {
        public RedisBulkStringAggregator() : base(RedisConstants.RedisMessageMaxLength)
        {
        }

        protected override bool IsStartMessage(IRedisMessage msg) => msg is BulkStringHeaderRedisMessage && !this.IsAggregated(msg);

        protected override bool IsContentMessage(IRedisMessage msg) => msg is IBulkStringRedisContent;

        protected override bool IsLastContentMessage(IBulkStringRedisContent msg) => msg is ILastBulkStringRedisContent;

        protected override bool IsAggregated(IRedisMessage msg) => msg is IFullBulkStringRedisMessage;

        protected override bool IsContentLengthInvalid(BulkStringHeaderRedisMessage start, int maxContentLength) => start.BulkStringLength > maxContentLength;

        protected override object NewContinueResponse(BulkStringHeaderRedisMessage start, int maxContentLength, IAgentPipeline pipeline) => null;

        protected override bool CloseAfterContinueResponse(object msg) => throw new NotSupportedException();

        protected override bool IgnoreContentAfterContinueResponse(object msg) => throw new NotSupportedException();

        protected override IFullBulkStringRedisMessage BeginAggregation(BulkStringHeaderRedisMessage start, IByteBuffer content) => new FullBulkStringRedisMessage(content);
    }
}