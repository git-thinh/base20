using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    // https://theburningmonk.com/2010/02/threading-using-readerwriterlockslim/
    // https://msdn.microsoft.com/en-us/library/system.threading.readerwriterlockslim(v=vs.110).aspx
    // https://stackoverflow.com/questions/170028/how-would-you-simplify-entering-and-exiting-a-readerwriterlock
    /*
private static readonly ReaderWriteLockSlim rwlock = new ReaderWriterLockSlim();
…
rwlock.EnterReadLock()
try
{
     // do something here
}
finally
{
     // don't forget to release the lock afterwards!
     rwlock.ExitReadLock();
}


using (_sync.Read())
{
     // do reading here
}
using (_sync.Write())
{
     // do writing here
}

     */

    public static class ReaderWriterLockSlimExtensions
    {
        private sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterReadLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitReadLock();
                    _sync = null;
                }
            }
        }

        private sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterWriteLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitWriteLock();
                    _sync = null;
                }
            }
        }

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            return new ReadLockToken(obj);
        }
        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            return new WriteLockToken(obj);
        }
    }
}
