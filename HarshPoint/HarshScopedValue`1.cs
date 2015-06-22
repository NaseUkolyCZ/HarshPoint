﻿using System;

namespace HarshPoint
{
    public sealed class HarshScopedValue<T>
    {
        public HarshScopedValue()
        {
        }

        public HarshScopedValue(T value)
        {
            Value = value;
        }

        public Scope Enter(T value)
        {
            var scope = new Scope(this, Value);
            Value = value;
            return scope;
        }

        public T Value
        {
            get;
            private set;
        }

        public struct Scope : IDisposable
        {
            private T OldValue;
            private HarshScopedValue<T> Owner;

            internal Scope(HarshScopedValue<T> owner, T oldValue)
            {
                Owner = owner;
                OldValue = oldValue;
            }

            public void Dispose()
            {
                if (Owner != null)
                {
                    Owner.Value = OldValue;

                    Owner = null;
                    OldValue = default(T);
                }
            }
        }
    }
}
