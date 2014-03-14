using System;
using System.Runtime.Remoting.Messaging;

namespace Nito.AsyncEx.AsyncLocal
{
    /// <summary>
    /// Data that is "local" to the current async method. This is the async near-equivalent of <c>ThreadLocal&lt;T&gt;</c>.
    /// </summary>
    /// <typeparam name="TImmutableType">The type of the data. This must be an immutable type.</typeparam>
    public sealed class AsyncLocal<TImmutableType> : IDisposable
    {
        /// <summary>
        /// Our unique slot name.
        /// </summary>
        private readonly string _slotName = Guid.NewGuid().ToString("N");

        /// <summary>
        /// The default value factory.
        /// </summary>
        private readonly Func<TImmutableType> _factory;

        /// <summary>
        /// Creates a new async-local variable that lazy-initializes its value with the default value of <typeparamref name="TImmutableType"/>.
        /// </summary>
        public AsyncLocal()
            : this(() => default(TImmutableType))
        {
        }

        /// <summary>
        /// Creates a new async-local variable that lazy-initializes its value with the specified value.
        /// </summary>
        /// <param name="empty">The value used to lazy-initialize <see cref="Value"/>.</param>
        public AsyncLocal(TImmutableType empty)
            : this(() => empty)
        {
        }

        /// <summary>
        /// Creates a new async-local variable that uses the specified factory method to lazy-initialize itself.
        /// </summary>
        /// <param name="factory"></param>
        public AsyncLocal(Func<TImmutableType> factory)
        {
            _factory = factory;
        }

        public bool IsValueCreated
        {
            get { return CallContext.LogicalGetData(_slotName) != null; }
        }

        /// <summary>
        /// Gets or sets the value of this async-local variable.
        /// </summary>
        public TImmutableType Value
        {
            get
            {
                var ret = CallContext.LogicalGetData(_slotName) as Wrapper;
                if (ret != null)
                    return ret.Value;
                
                // When there is no value yet for this logical call context, then this thread is about to create its own copy of the context.
                // So we can create the value directly without worrying about multiple threads executing the factory method.
                ret = new Wrapper(_factory());
                CallContext.LogicalSetData(_slotName, ret);
                return ret.Value;
            }

            set
            {
                CallContext.LogicalSetData(_slotName, new Wrapper(value));
            }
        }

        /// <summary>
        /// Deletes this async-local variable.
        /// </summary>
        public void Dispose()
        {
            CallContext.FreeNamedDataSlot(_slotName);
        }

        private sealed class Wrapper
        {
            public Wrapper(TImmutableType value)
            {
                Value = value;
            }

            public TImmutableType Value { get; private set; }
        }
    }
}