// Needed for Workaround

using System;
using System.Threading;

namespace Theraot.Threading.Needles
{
    [Serializable]
    [System.Diagnostics.DebuggerNonUserCode]
    public class PromiseNeedle : IWaitablePromise
    {
        private readonly int _hashCode;
        private Exception _exception;
        private StructNeedle<ManualResetEventSlim> _waitHandle;

        public PromiseNeedle(bool done)
        {
            _exception = null;
            _hashCode = base.GetHashCode();
            if (!done)
            {
                _waitHandle = new ManualResetEventSlim(false);
            }
        }

        public PromiseNeedle(Exception exception)
        {
            _exception = exception;
            _hashCode = exception.GetHashCode();
            _waitHandle = new ManualResetEventSlim(true);
        }

        ~PromiseNeedle()
        {
            ReleaseWaitHandle(false);
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }

        bool IPromise.IsCanceled
        {
            get
            {
                return false;
            }
        }

        public bool IsCompleted
        {
            get
            {
                var waitHandle = _waitHandle.Value;
                return waitHandle == null || waitHandle.IsSet;
            }
        }

        public bool IsFaulted
        {
            get
            {
                return _exception != null;
            }
        }

        protected IRecyclableNeedle<ManualResetEventSlim> WaitHandle
        {
            get
            {
                return _waitHandle;
            }
        }

        public virtual void Free()
        {
            if (_waitHandle.IsAlive)
            {
                _waitHandle.Value.Reset();
            }
            else
            {
                _waitHandle.Value = new ManualResetEventSlim(false);
            }
            _exception = null;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public void SetCompleted()
        {
            _exception = null;
            ReleaseWaitHandle(true);
        }

        public void SetError(Exception error)
        {
            _exception = error;
            ReleaseWaitHandle(true);
        }

        public override string ToString()
        {
            return IsCompleted
                ? (ReferenceEquals(_exception, null)
                    ? "[Done]"
                    : _exception.ToString())
                : "[Not Created]";
        }

        public virtual void Wait()
        {
            var waitHandle = _waitHandle.Value;
            if (waitHandle != null)
            {
                try
                {
                    waitHandle.Wait();
                }
                catch (ObjectDisposedException exception)
                {
                    // Came late to the party, initialization was done
                    GC.KeepAlive(exception);
                }
            }
        }

        protected void ReleaseWaitHandle(bool done)
        {
            var waitHandle = _waitHandle.Value;
            if (!ReferenceEquals(waitHandle, null))
            {
                if (done)
                {
                    waitHandle.Set();
                }
                waitHandle.Dispose();
            }
            _waitHandle.Value = null;
        }
    }
}