﻿#if FAT

using System;

using Theraot.Core;
using Theraot.Threading;

namespace Theraot.Threading.Needles
{
    public sealed partial class Transaction
    {
        public sealed partial class TransactionNeedle<T> : IDisposable
        {
            private int _status;

            [global::System.Diagnostics.DebuggerNonUserCode]
            [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralexceptionTypes", Justification = "Pokemon")]
            ~TransactionNeedle()
            {
                try
                {
                    //Empty
                }
                finally
                {
                    try
                    {
                        Dispose(false);
                    }
                    catch
                    {
                        //Pokemon
                    }
                }
            }

            [global::System.Diagnostics.DebuggerNonUserCode]
            public void Dispose()
            {
                try
                {
                    Dispose(true);
                }
                finally
                {
                    GC.SuppressFinalize(this);
                }
            }

            [global::System.Diagnostics.DebuggerNonUserCode]
            private void Dispose(bool disposeManagedResources)
            {
                if (TakeDisposalExecution())
                {
                    try
                    {
                        if (disposeManagedResources)
                        {
                            this.OnDispose();
                        }
                    }
                    finally
                    {
                        try
                        {
                            //Empty
                        }
                        finally
                        {
                            _value = null;
                        }
                    }
                }
            }

            private bool TakeDisposalExecution()
            {
                if (_status == -1)
                {
                    return false;
                }
                else
                {
                    ThreadingHelper.SpinWaitExchange(ref _status, -1, 0, -1);
                    if (_status == -1)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
    }
}

#endif
