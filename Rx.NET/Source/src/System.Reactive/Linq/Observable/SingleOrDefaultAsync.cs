﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive.Linq.ObservableImpl
{
    internal static class SingleOrDefaultAsync<TSource>
    {
        internal sealed class Sequence : Producer<TSource>
        {
            private readonly IObservable<TSource> _source;

            public Sequence(IObservable<TSource> source)
            {
                _source = source;
            }

            protected override IDisposable Run(IObserver<TSource> observer, IDisposable cancel, Action<IDisposable> setSink)
            {
                var sink = new _(observer, cancel);
                setSink(sink);
                return _source.SubscribeSafe(sink);
            }

            private sealed class _ : Sink<TSource>, IObserver<TSource>
            {
                private TSource _value;
                private bool _seenValue;

                public _(IObserver<TSource> observer, IDisposable cancel)
                    : base(observer, cancel)
                {
                    _value = default(TSource);
                    _seenValue = false;
                }

                public void OnNext(TSource value)
                {
                    if (_seenValue)
                    {
                        base._observer.OnError(new InvalidOperationException(Strings_Linq.MORE_THAN_ONE_ELEMENT));
                        base.Dispose();
                        return;
                    }

                    _value = value;
                    _seenValue = true;
                }

                public void OnError(Exception error)
                {
                    base._observer.OnError(error);
                    base.Dispose();
                }

                public void OnCompleted()
                {
                    base._observer.OnNext(_value);
                    base._observer.OnCompleted();
                    base.Dispose();
                }
            }
        }

        internal sealed class Predicate : Producer<TSource>
        {
            private readonly IObservable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;

            public Predicate(IObservable<TSource> source, Func<TSource, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            protected override IDisposable Run(IObserver<TSource> observer, IDisposable cancel, Action<IDisposable> setSink)
            {
                var sink = new _(_predicate, observer, cancel);
                setSink(sink);
                return _source.SubscribeSafe(sink);
            }

            private sealed class _ : Sink<TSource>, IObserver<TSource>
            {
                private readonly Func<TSource, bool> _predicate;
                private TSource _value;
                private bool _seenValue;

                public _(Func<TSource, bool> predicate, IObserver<TSource> observer, IDisposable cancel)
                    : base(observer, cancel)
                {
                    _predicate = predicate;

                    _value = default(TSource);
                    _seenValue = false;
                }

                public void OnNext(TSource value)
                {
                    var b = false;

                    try
                    {
                        b = _predicate(value);
                    }
                    catch (Exception ex)
                    {
                        base._observer.OnError(ex);
                        base.Dispose();
                        return;
                    }

                    if (b)
                    {
                        if (_seenValue)
                        {
                            base._observer.OnError(new InvalidOperationException(Strings_Linq.MORE_THAN_ONE_MATCHING_ELEMENT));
                            base.Dispose();
                            return;
                        }

                        _value = value;
                        _seenValue = true;
                    }
                }

                public void OnError(Exception error)
                {
                    base._observer.OnError(error);
                    base.Dispose();
                }

                public void OnCompleted()
                {
                    base._observer.OnNext(_value);
                    base._observer.OnCompleted();
                    base.Dispose();
                }
            }
        }
    }
}
