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
using System.Threading.Tasks;

namespace PagerDutyAPI {
    // Quick Either class
    public static class Either {
        public static IEither<L,R> Left<L,R>(L value) {
            return new ILeft<L,R>(value);
        }
        public static IEither<L,R> Right<L,R>(R value) {
            return new IRight<L,R>(value);
        }
    }

    public interface IEither<L, R> {
        void OnLeft(Action<L> f);
        void OnRight(Action<R> f);
    }
    class ILeft<L,R>: IEither<L,R> {
        L value;
        public ILeft(L value) { this.value = value; }
        public void OnLeft(Action<L> f) { f(value); }
        public void OnRight(Action<R> f) { }
    }
    class IRight<L,R> : IEither<L,R> {
        R value;
        public IRight(R value) { this.value = value; }
        public void OnLeft(Action<L> f) { }
        public void OnRight(Action<R> f) { f(value); }
    }
}
