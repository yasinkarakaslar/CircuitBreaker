﻿//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test
{
    public class HystrixThreadEventStreamTest : CommandStreamTest
    {
        class LatchedObserver<T> : ObserverBase<T>
        {
            CountdownEvent latch;
            public LatchedObserver(CountdownEvent latch)
            {
                this.latch = latch;
            }

            protected override void OnCompletedCore()
            {
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(T value)
            {
            }
        }

        IHystrixCommandKey commandKey;
        IHystrixThreadPoolKey threadPoolKey;

        HystrixThreadEventStream writeToStream;
        HystrixCommandCompletionStream readCommandStream;
        HystrixThreadPoolCompletionStream readThreadPoolStream;
        ITestOutputHelper output;

        public HystrixThreadEventStreamTest(ITestOutputHelper output) : base()
        {
            this.output = output;
            commandKey = HystrixCommandKeyDefault.AsKey("CMD-ThreadStream");
            threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("TP-ThreadStream");

            writeToStream = HystrixThreadEventStream.GetInstance();
            readCommandStream = HystrixCommandCompletionStream.GetInstance(commandKey);
            readThreadPoolStream = HystrixThreadPoolCompletionStream.GetInstance(threadPoolKey);
        }



        [Fact]
        public void NoEvents()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            //no writes

            Assert.False(commandLatch.Wait(TimeSpan.FromMilliseconds(1000)));
            Assert.False(threadPoolLatch.Wait(TimeSpan.FromMilliseconds(1000)));
        }

        [Fact]
        public void TestThreadIsolatedSuccess()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.True(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestSemaphoreIsolatedSuccess()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }
        [Fact]
        public void TestThreadIsolatedFailure()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.FAILURE).SetExecutedInThread();
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.True(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestSemaphoreIsolatedFailure()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.FAILURE);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestThreadIsolatedTimeout()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.TIMEOUT).SetExecutedInThread();
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.True(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestSemaphoreIsolatedTimeout()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.TIMEOUT);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestThreadIsolatedBadRequest()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.BAD_REQUEST).SetExecutedInThread();
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.True(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestSemaphoreIsolatedBadRequest()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.BAD_REQUEST);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestThreadRejectedCommand()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.THREAD_POOL_REJECTED);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.True(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestSemaphoreRejectedCommand()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SEMAPHORE_REJECTED);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestThreadIsolatedResponseFromCache()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<IList<HystrixCommandCompletion>> commandListSubscriber = new LatchedObserver<IList<HystrixCommandCompletion>>(commandLatch);
            readCommandStream.Observe().Buffer(TimeSpan.FromMilliseconds(500)).Take(1)
                            .Do((hystrixCommandCompletions) =>
                            {
                                output.WriteLine("LIST : " + hystrixCommandCompletions);
                                Assert.Equal(3, hystrixCommandCompletions.Count);
                            }).Subscribe(commandListSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            ExecutionResult cache1 = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE);
            ExecutionResult cache2 = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);
            writeToStream.ExecutionDone(cache1, commandKey, threadPoolKey);
            writeToStream.ExecutionDone(cache2, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.True(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestSemaphoreIsolatedResponseFromCache()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<IList<HystrixCommandCompletion>> commandListSubscriber = new LatchedObserver<IList<HystrixCommandCompletion>>(commandLatch);
            readCommandStream.Observe().Buffer(TimeSpan.FromMilliseconds(500)).Take(1)
                            .Do((hystrixCommandCompletions) =>
                            {
                                output.WriteLine("LIST : " + hystrixCommandCompletions);
                                Assert.Equal(3, hystrixCommandCompletions.Count);
                            })
                            .Subscribe(commandListSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS);
            ExecutionResult cache1 = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE);
            ExecutionResult cache2 = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);
            writeToStream.ExecutionDone(cache1, commandKey, threadPoolKey);
            writeToStream.ExecutionDone(cache2, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }

        [Fact]
        public void TestShortCircuit()
        {
            CountdownEvent commandLatch = new CountdownEvent(1);
            CountdownEvent threadPoolLatch = new CountdownEvent(1);

            IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
            readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

            IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
            readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SHORT_CIRCUITED);
            writeToStream.ExecutionDone(result, commandKey, threadPoolKey);

            Assert.True(commandLatch.Wait(1000));
            Assert.False(threadPoolLatch.Wait(1000));
        }
    }
}
