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
    /// Represents a Last-In, First-Out thread-safe collection of objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StackThreadSafe<T> : IEnumerable<T>, ICollection
    {
        #region Variables

        /// <summary>
        /// internal stack
        /// </summary>
        private readonly Stack<T> m_stack;

        /// <summary>
        /// lock for accessing the stack
        /// </summary>
        private readonly ReaderWriterLockSlim LockStack = new ReaderWriterLockSlim();

        /// <summary>
        /// Used only for the SyncRoot properties
        /// </summary>
        private readonly object objSyncRoot = new object();

        // Variables
        #endregion

        #region Init

        /// <summary>
        /// Initializes a new instance of the <see cref="StackThreadSafe{T}"/> class.
        /// </summary>
        public StackThreadSafe()
        {
            m_stack = new Stack<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackThreadSafe{T}"/> class.
        /// </summary>
        /// <param name="col">
        /// The starting collection
        /// </param>
        public StackThreadSafe(IEnumerable<T> col)
        {
            m_stack = new Stack<T>(col);
        }

        // Init
        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// The get enumerator
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            Stack<T> LocalStack = null;

            // init enumerator
            LockStack.PerformUsingReadLock(() =>
            {
                // create a copy of m_TList
                LocalStack = new Stack<T>(m_stack);
            });

            // get the enumerator
            foreach (T item in LocalStack)
                yield return item;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// The get enumerator
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            Stack<T> LocalStack = null;

            // init enumerator
            LockStack.PerformUsingReadLock(()=>
            {
                // create a copy of m_TList
                LocalStack = new Stack<T>(m_stack);
            });

            // get the enumerator
            foreach (T item in LocalStack)
                yield return item;
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Copy to an array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(Array array, int index)
        {
            LockStack.PerformUsingReadLock(()=> m_stack.ToArray().CopyTo(array, index));
        }

        /// <summary>
        /// Number of items in the Stack
        /// </summary>
        public int Count
        {
            get
            {
                return LockStack.PerformUsingReadLock(()=> m_stack.Count);
            }
        }

        /// <summary>
        /// Always true
        /// </summary>
        public bool IsSynchronized
        {
            get { return true; }
        }

        /// <summary>
        /// Sync root
        /// </summary>
        public object SyncRoot
        {
            get { return objSyncRoot; }
        }

        #endregion

        #region Stack_Funcs

        #region Clear

        /// <summary>
        /// Clears the collection
        /// </summary>
        public void Clear()
        {
            LockStack.PerformUsingWriteLock(()=> m_stack.Clear());
        }

        // Clear
        #endregion

        #region Contains

        /// <summary>
        /// True if the item is in the Stack
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return LockStack.PerformUsingReadLock(() => m_stack.Contains(item));
        }

        // Contains
        #endregion

        #region Peek

        /// <summary>
        /// Returns top item in the Stack, without removing it from the Stack
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            return LockStack.PerformUsingReadLock(()=> m_stack.Peek());
        }

        // Peek
        #endregion

        #region Pop

        /// <summary>
        /// Removes and returns the top item in the Stack
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            return LockStack.PerformUsingWriteLock(()=> m_stack.Pop());
        }

        // Pop
        #endregion

        #region Push

        /// <summary>
        /// Inserts an item into the Stack
        /// </summary>
        /// <param name="item"></param>
        public void Push(T item)
        {
            LockStack.PerformUsingWriteLock(() => m_stack.Push(item));
        }

        // Push
        #endregion

        #region ToArray

        /// <summary>
        /// Convert the Stack to an array
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            return LockStack.PerformUsingReadLock(() => m_stack.ToArray());
        }

        // ToArray
        #endregion

        #region TrimExcess

        /// <summary>
        /// Sets the capacity to the actual number of elements in the Stack
        /// </summary>
        public void TrimExcess()
        {
            LockStack.PerformUsingWriteLock(() => m_stack.TrimExcess());
        }

        // TrimExcess
        #endregion

        // Stack_Funcs
        #endregion
    }
}
