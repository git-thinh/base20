using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

/*
ThreadSafeCollections

Copyright Philip Pierce © 2010 - 2014

I’ve been doing a lot of mult-threading work, recently, using the standard Thead 
classes, the Worker Queue, PLINQ (Parallel LINQ), and new TPL (Task Parallel Lirary). 
The problem with most of the built-in generic collections (Queue<>, List<>, Dictionary<>, etc), 
is that they are not thread safe. There is a thread-safe collection provided by .NET, but it's
klunky and doesn't support everything I need.

I created a library of thread safe collections which allow me to use the standard 
generic collection actions (foreach, LINQ, etc), while at the same time being thread safe.

The classes in this library inherit from the appropriate collection interface 
(IEnumerable, ICollection, etc). Each class also has all the functions and properties 
that it’s original non-thread safe class has, including new functions (such as
AddIfNotExistElseUpdate for TDictionary).

I've also included an extension to the ReaderWriterLockSlim, which allows me to wrap
Read / Write locks in a more user-friendly manner. In addition, the extensions handle
the issue of always having try/finally blocks on the locks.

I use the ReaderWriterLockSlim a lot, because of its versatility in locking, it's small
use of resources, and its speed. 
*/

namespace System.Threading
{
    /// <summary>
    /// Represents a First-In, First-Out thread-safe collection of objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueThreadSafe<T> : ICollection
    {
        #region Variables

        /// <summary>
        /// The private q which holds the actual data
        /// </summary>
        private readonly Queue<T> m_Queue;

        /// <summary>
        /// Lock for the Q
        /// </summary>
        private readonly ReaderWriterLockSlim LockQ = new ReaderWriterLockSlim();

        /// <summary>
        /// Used only for the SyncRoot properties
        /// </summary>
        private readonly object objSyncRoot = new object();

        // Variables
        #endregion

        #region Init

        /// <summary>
        /// Initializes the Queue
        /// </summary>
        public QueueThreadSafe()
        {
            m_Queue = new Queue<T>();
        }

        /// <summary>
        /// Initializes the Queue
        /// </summary>
        /// <param name="capacity">the initial number of elements the queue can contain</param>
        public QueueThreadSafe(int capacity)
        {
            m_Queue = new Queue<T>(capacity);
        }

        /// <summary>
        /// Initializes the Queue
        /// </summary>
        /// <param name="collection">the collection whose members are copied to the Queue</param>
        public QueueThreadSafe(IEnumerable<T> collection)
        {
            m_Queue = new Queue<T>(collection);
        }

        // Init
        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that enumerates through the collection
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            Queue<T> localQ;

            // init enumerator
            LockQ.EnterReadLock();
            try
            {
                // create a copy of m_TList
                localQ = new Queue<T>(m_Queue);
            }
            finally
            {
                LockQ.ExitReadLock();
            }

            // get the enumerator
            foreach (T item in localQ)
                yield return item;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that enumerates through the collection
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            Queue<T> localQ;

            // init enumerator
            LockQ.EnterReadLock();
            try
            {
                // create a copy of m_TList
                localQ = new Queue<T>(m_Queue);
            }
            finally
            {
                LockQ.ExitReadLock();
            }

            // get the enumerator
            foreach (T item in localQ)
                yield return item;
        }

        #endregion

        #region ICollection Members

        #region CopyTo

        /// <summary>
        /// Copies the Queue's elements to an existing array
        /// </summary>
        /// <param name="array">the one-dimensional array to copy into</param>
        /// <param name="index">the zero-based index at which copying begins</param>
        public void CopyTo(Array array, int index)
        {
            LockQ.EnterReadLock();
            try
            {
                // copy
                m_Queue.ToArray().CopyTo(array, index);
            }

            finally
            {
                LockQ.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the Queue's elements to an existing array
        /// </summary>
        /// <param name="array">the one-dimensional array to copy into</param>
        /// <param name="index">the zero-based index at which copying begins</param>
        public void CopyTo(T[] array, int index)
        {
            LockQ.EnterReadLock();
            try
            {
                // copy
                m_Queue.CopyTo(array, index);
            }

            finally
            {
                LockQ.ExitReadLock();
            }
        }

        // CopyTo
        #endregion

        #region Count

        /// <summary>
        /// Returns the number of items in the Queue
        /// </summary>
        public int Count
        {
            get
            {
                LockQ.EnterReadLock();
                try
                {
                    return m_Queue.Count;
                }

                finally
                {
                    LockQ.ExitReadLock();
                }
            }
        }

        // Count
        #endregion

        #region IsSynchronized

        /// <summary>
        /// Synchronized - true
        /// </summary>
        public bool IsSynchronized
        {
            get { return true; }
        }

        // IsSynchronized
        #endregion

        #region SyncRoot

        /// <summary>
        /// Sync root
        /// </summary>
        public object SyncRoot
        {
            get { return objSyncRoot; }
        }

        // SyncRoot
        #endregion

        #endregion

        #region Enqueue

        /// <summary>
        /// Adds an item to the queue
        /// </summary>
        /// <param name="item">the item to add to the queue</param>
        public void Enqueue(T item)
        {
            LockQ.EnterWriteLock();
            try
            {
                m_Queue.Enqueue(item);
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds an item to the queue, returns the number of items in queue, after 
        /// this item has been added
        /// </summary>
        /// <param name="item">the item to add to the queue</param>
        public int EnqueueAndCount(T item)
        {
            LockQ.EnterWriteLock();
            try
            {
                m_Queue.Enqueue(item);
                return m_Queue.Count;
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        // Enqueue
        #endregion

        #region Dequeue

        /// <summary>
        /// Removes and returns the item in the beginning of the queue
        /// </summary>
        public T Dequeue(T _defaultIfEmpty)
        {
            LockQ.EnterWriteLock();
            try
            {
                if (m_Queue.Count > 0)
                   return m_Queue.Dequeue();
            }
            finally
            {
                LockQ.ExitWriteLock();
            }
            return _defaultIfEmpty;
        }

        // Dequeue
        #endregion

        #region EnqueueAll

        /// <summary>
        /// Enqueues the list of items
        /// </summary>
        /// <param name="ItemsToQueue">list of items to enqueue</param>
        public void EnqueueAll(IEnumerable<T> ItemsToQueue)
        {
            LockQ.EnterWriteLock();
            try
            {
                // loop through and add each item
                foreach (T item in ItemsToQueue)
                    m_Queue.Enqueue(item);
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        /// <summary>
        /// Enqueues the list of items
        /// </summary>
        /// <param name="ItemsToQueue">list of items to enqueue</param>
        public void EnqueueAll(ListThreadSafe<T> ItemsToQueue)
        {
            LockQ.EnterWriteLock();
            try
            {
                // loop through and add each item
                foreach (T item in ItemsToQueue)
                    m_Queue.Enqueue(item);
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        // EnqueueAll
        #endregion

        #region DequeueAll

        /// <summary>
        /// Dequeues all the items and returns them as a thread safe list
        /// </summary>
        public ListThreadSafe<T> DequeueAll()
        {
            LockQ.EnterWriteLock();
            try
            {
                // create return object
                ListThreadSafe<T> returnList = new ListThreadSafe<T>();

                // dequeue until everything is out
                while (m_Queue.Count > 0)
                    returnList.Add(m_Queue.Dequeue());

                // return the list
                return returnList;
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        // DequeueAll
        #endregion

        #region DequeueAllList

        /// <summary>
        /// Dequeues all the items and returns them as a thread safe list
        /// </summary>
        public List<T> DequeueAllList()
        {
            LockQ.EnterWriteLock();
            try
            {
                // create return object
                List<T> returnList = new List<T>();

                // dequeue until everything is out
                while (m_Queue.Count > 0)
                    returnList.Add(m_Queue.Dequeue());

                // return the list
                return returnList;
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        // DequeueAllList
        #endregion

        #region Clear

        /// <summary>
        /// Removes all items from the queue
        /// </summary>
        public void Clear()
        {
            LockQ.EnterWriteLock();
            try
            {
                m_Queue.Clear();
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        // Clear
        #endregion

        #region Contains

        /// <summary>
        /// Determines whether the item exists in the Queue
        /// </summary>
        /// <param name="item">the item to find in the queue</param>
        public bool Contains(T item)
        {
            LockQ.EnterReadLock();
            try
            {
                return m_Queue.Contains(item);
            }

            finally
            {
                LockQ.ExitReadLock();
            }
        }

        // Contains
        #endregion

        #region Peek

        /// <summary>
        /// Returns the item at the start of the Queue without removing it
        /// </summary>
        public T Peek()
        {
            LockQ.EnterReadLock();
            try
            {
                return m_Queue.Peek();
            }

            finally
            {
                LockQ.ExitReadLock();
            }
        }

        // Peek
        #endregion

        #region ToArray

        /// <summary>
        /// Copies the Queue to a new array
        /// </summary>
        public T[] ToArray()
        {
            LockQ.EnterReadLock();
            try
            {
                return m_Queue.ToArray();
            }

            finally
            {
                LockQ.ExitReadLock();
            }
        }

        // ToArray
        #endregion

        #region TrimExcess

        /// <summary>
        /// Sets the capacity of the Queue to the current number of items, if that number
        /// is less than 90 percent of the current capacity
        /// </summary>
        public void TrimExcess()
        {
            LockQ.EnterWriteLock();
            try
            {
                m_Queue.TrimExcess();
            }

            finally
            {
                LockQ.ExitWriteLock();
            }
        }

        // TrimExcess
        #endregion
    }
}
