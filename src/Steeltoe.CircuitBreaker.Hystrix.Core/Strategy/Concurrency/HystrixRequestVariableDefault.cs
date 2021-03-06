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
//

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;


namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixRequestVariableDefault<T> : IHystrixRequestVariable<T>
    {
        private Action<T> _disposeAction;
        private Func<T> _valueFactory;
        public HystrixRequestVariableDefault(T value)
        {
            _valueFactory = () => { return value; };
        }

        public HystrixRequestVariableDefault(Func<T> valueFactory, Action<T> disposeAction) 
        {
            _valueFactory = valueFactory;
            _disposeAction = disposeAction;
        }

        public HystrixRequestVariableDefault(Func<T> valueFactory)
        {
            _valueFactory = valueFactory;
        }


        public virtual T Value
        {
            get
            {
                // Checks to make sure HystrixRequestContext.ContextForCurrentThread.State != null
                if (!HystrixRequestContext.IsCurrentThreadInitialized)
                    throw new InvalidOperationException("HystrixRequestContext.InitializeContext() must be called at the beginning of each request before RequestVariable functionality can be used.");

                return (T)HystrixRequestContext.ContextForCurrentThread.State.GetOrAddEx(this, (k) => _valueFactory());
            }

        }

        internal virtual void Remove()
        {
            if (HystrixRequestContext.ContextForCurrentThread != null)
            {
                Remove(HystrixRequestContext.ContextForCurrentThread, this);

            }
        }

        internal static void Remove(HystrixRequestContext context, IHystrixRequestVariable<T> v)
        {
            object oldValue;
            if (context.State.TryRemove(v, out oldValue))
            {
                v.Dispose();
            }
        }

        public virtual void Dispose()
        {
            if (_disposeAction != null)
            {
                _disposeAction(Value);
            }
        }
    }

}
