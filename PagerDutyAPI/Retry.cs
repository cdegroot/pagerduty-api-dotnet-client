/*
 * Copyright (c) 2015 Cees de Groot
 * Apache License, Version 2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PagerDutyAPI {
    // <summary>
    // A Retry class. Shamelessly stolen from Stack Overflow and then hacked up for a bit. 
    // http://stackoverflow.com/questions/1563191/c-sharp-cleanest-way-to-write-retry-logic
    // </summary>
    public class Retry {
        readonly TimeSpan retryInterval;
        readonly int retryCount;
        readonly bool exponentialBackoff;

        public Retry(TimeSpan retryInterval, int retryCount = 3, bool exponentialBackoff = true) {
            this.retryInterval = retryInterval;
            this.retryCount = retryCount;
            this.exponentialBackoff = exponentialBackoff;
        }

        // <summary>
        // Execute the function under retry logic
        // </summary>
        public R Do<R>(Func<IEither<Exception, R>> fun) {
            var nothing = default(R);
            var exceptions = new List<Exception>();
            var currentTimeout = retryInterval;
            for (int retry = 0; retry < retryCount; retry++) {
                var response = fun();
                R result = nothing;
				response.OnLeft(exceptions.Add);
                response.OnRight((r) => result = r);

                if (!ReferenceEquals(result, nothing)) {
                    return result;
                } else {
                    Thread.Sleep(currentTimeout);
                    currentTimeout = exponentialBackoff ? 
                        currentTimeout + currentTimeout : 
                        currentTimeout;                
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
