using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
//using Salar.Bois;

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
    /// Represents a threaded List
    /// </summary>
    /// <typeparam name="T"> </typeparam>
    public class ListThreadSafe<T> : IList<T>
    {
        #region Variables

        /// <summary>
        /// The container list that holds the actual data
        /// </summary>
        //private readonly List<T> m_TList;
        private List<T> m_TList;

        /// <summary>
        /// The lock used when accessing the list
        /// </summary>
        private readonly ReaderWriterLockSlim LockList = new ReaderWriterLockSlim();
        //private readonly BoisSerializer boisSerializer = new BoisSerializer();

        // Variables
        #endregion

        #region Properties

        /// <summary>
        /// When true, we have already disposed of the object
        /// </summary>
        private int m_Disposed;

        /// <summary>
        /// When true, we have already disposed of the object
        /// </summary>
        private bool Disposed
        {
            get { return Thread.VolatileRead(ref m_Disposed) == 1; }
            set { Thread.VolatileWrite(ref m_Disposed, value ? 1 : 0); }
        }

        // Properties
        #endregion

        #region Init

        /// <summary>
        /// Creates an empty threaded list object
        /// </summary>
        public ListThreadSafe()
        {
            m_TList = new List<T>();
        }

        /// <summary>
        /// Creates an empty threaded list object
        /// </summary>
        /// <param name="capacity">the number of elements the list can initially store</param>
        public ListThreadSafe(int capacity)
        {
            m_TList = new List<T>(capacity);
        }

        /// <summary>
        /// Creates an empty threaded list object
        /// </summary>
        /// <param name="collection">a collection of objects which are copied into the threaded list</param>
        public ListThreadSafe(IEnumerable<T> collection)
        {
            m_TList = new List<T>(collection);
        }

        // Init
        #endregion

        #region TypeCast_Overloads

        /// <summary>
        /// Converts List to TList
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator ListThreadSafe<T>(List<T> value)
        {
            return new ListThreadSafe<T>(value);
        }

        // TypeCast_Overloads
        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator to iterate through the collection
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            // http://www.csharphelp.com/archives/archive181.html

            List<T> localList;

            // init enumerator
            LockList.EnterReadLock();
            try
            {
                // create a copy of m_TList
                localList = new List<T>(m_TList);
            }
            finally
            {
                LockList.ExitReadLock();
            }

            // get the enumerator
            foreach (T item in localList)
                yield return item;
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator to iterate through the collection
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            // http://www.csharphelp.com/archives/archive181.html

            List<T> localList;

            // init enumerator
            LockList.EnterReadLock();
            try
            {
                // create a copy of m_TList
                localList = new List<T>(m_TList);
            }
            finally
            {
                LockList.ExitReadLock();
            }

            // get the enumerator
            foreach (T item in localList)
                yield return item;
        }

        #endregion

        #region IDisposable Members

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.

        /// <summary>
        /// Dispose of the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.

        /// <summary>
        /// The disposer
        /// </summary>
        /// <param name="disposing">true if disposed</param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (this.Disposed) return;

            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            //if(disposing)
            //{
            //    // NO managed resources to dispose for this object
            //}

            // Note disposing has been done.
            Disposed = true;
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.

        /// <summary>
        /// Finalizes an instance of the <see cref="ListThreadSafe{T}"/> class. 
        /// </summary>
        ~ListThreadSafe()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion

        #region Add

        /// <summary>
        /// Adds an item to the threaded list
        /// </summary>
        /// <param name="item">the item to add to the end of the collection</param>
        public void Add(T item)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Add(item);
            }

            finally
            {
                LockList.ExitWriteLock();
            }
        }

        // Add
        #endregion

        #region AddRange

        /// <summary>
        /// Adds the elements of collection to the end of the threaded list
        /// </summary>
        /// <param name="collection">the collection to add to the end of the list</param>
        public void AddRange(IEnumerable<T> collection, bool distinct = false)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.AddRange(collection);
                if (distinct)
                {
                    T[] a = m_TList.Distinct().ToArray();
                    m_TList.Clear();
                    m_TList.AddRange(a);
                }
            }

            finally
            {
                LockList.ExitWriteLock();
            }
        }

        public T[] AddRangeIfNotExist(T[] items)
        {
            if (items.Length == 0) return items;

            LockList.EnterWriteLock();
            try
            {
                if (m_TList.Count == 0)
                {
                    m_TList.AddRange(items);
                    return items;
                }

                items = items.Where(x => !m_TList.Any(p => p.Equals(x))).ToArray();
                m_TList.AddRange(items);

                return items;
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        public T Dequeue(T valueDefault)
        {
            LockList.EnterWriteLock();
            try
            {
                if (m_TList.Count > 0)
                {
                    T val = m_TList[0];
                    m_TList.RemoveAt(0);
                    return val;
                }
            }
            finally
            {
                LockList.ExitWriteLock();
            }
            return valueDefault;
        }
        // AddRange
        #endregion

        #region AddIfNotExist

        /// <summary>
        /// Adds an item to the threaded list if it is not already in the list.
        /// Returns true if added to the list, false if the item already existed
        /// in the list
        /// </summary>
        /// <param name="item">the item to add to the end of the collection</param>
        public bool AddIfNotExist(T item)
        {
            LockList.EnterWriteLock();
            try
            {
                // check if it exists already
                if (m_TList.Contains(item))
                    return false;

                // add the item and return true
                m_TList.Add(item);
                return true;
            }

            finally
            {
                LockList.ExitWriteLock();
            }
        }

        // AddIfNotExist
        #endregion

        #region AsReadOnly

        /// <summary>
        /// Returns a read-only collection of the current threaded list
        /// </summary>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.AsReadOnly();
            }

            finally
            {
                LockList.ExitReadLock();
            }
        }

        // AsReadOnly
        #endregion


        public void ReadFile(string file_path)
        {
            //LockList.PerformUsingWriteLock(() =>
            //{
            //    if (File.Exists(file_path))
            //    {
            //        using (var file = File.OpenRead(file_path))
            //        {
            //            //ProtoBuf.Serializer.Serialize<Dictionary<string, string>>(file, cacheData); 
            //            file.Position = 0;
            //            m_TList = boisSerializer.Deserialize<List<T>>(file);
            //            file.Close();
            //        }
            //    }
            //});
        }

        public void WriteFile(string file_path)
        {
            //LockList.PerformUsingWriteLock(() =>
            //{
            //    if (File.Exists(file_path))
            //    {
            //        // Using Protobuf-net, I suddenly got an exception about an unknown wire-type
            //        // https://stackoverflow.com/questions/2152978/using-protobuf-net-i-suddenly-got-an-exception-about-an-unknown-wire-type
            //        using (var file = new FileStream(file_path, FileMode.Truncate))
            //        {
            //            //ProtoBuf.Serializer.Serialize<Dictionary<string, string>>(file, cacheData);
            //            //file.SetLength(file.Position);
            //            //file.Close();

            //            boisSerializer.Serialize<List<T>>(m_TList, file);
            //            file.SetLength(file.Position);
            //            file.Close();
            //        }
            //    }
            //    else
            //    {
            //        using (var file = new FileStream(file_path, FileMode.OpenOrCreate))
            //        {
            //            //ProtoBuf.Serializer.Serialize<Dictionary<string, string>>(file, cacheData); 
            //            boisSerializer.Serialize<List<T>>(m_TList, file);
            //            file.Close();
            //        }
            //    }
            //});
        }

        #region BinarySearch

        /// <summary>
        /// Searches the collection using the default comparator and returns the zero-based index of the item found
        /// </summary>
        /// <param name="item">the item to search for</param>
        public int BinarySearch(T item)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.BinarySearch(item);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Searches the collection using the default comparator and returns the zero-based index of the item found
        /// </summary>
        /// <param name="item">the item to search for</param>
        /// <param name="comparer">the IComparer to use when searching, or null to use the default</param>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.BinarySearch(item, comparer);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Searches the collection using the default comparator and returns the zero-based index of the item found
        /// </summary>
        /// <param name="index">the zero-based index to start searching from</param>
        /// <param name="count">the number of records to search</param>
        /// <param name="item">the item to search for</param>
        /// <param name="comparer">the IComparer to use when searching, or null to use the default</param>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.BinarySearch(index, count, item, comparer);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // BinarySearch
        #endregion

        #region Capacity

        /// <summary>
        /// Gets or sets the initial capacity of the list
        /// </summary>
        public int Capacity
        {
            get
            {
                LockList.EnterReadLock();
                try
                {
                    return m_TList.Capacity;
                }

                finally
                {
                    LockList.ExitReadLock();
                }
            }
            set
            {
                LockList.EnterWriteLock();
                try
                {
                    m_TList.Capacity = value;
                }

                finally
                {
                    LockList.ExitWriteLock();
                }
            }
        }

        // Capacity
        #endregion

        #region Clear

        /// <summary>
        /// Removes all items from the threaded list
        /// </summary>
        public void Clear()
        {
            LockList.EnterReadLock();
            try
            {
                m_TList.Clear();
            }

            finally
            {
                LockList.ExitReadLock();
            }
        }

        // Clear
        #endregion

        #region Contains

        /// <summary>
        /// Returns true if the collection contains this item
        /// </summary>
        /// <param name="item">the item to find in the collection</param>
        public bool Contains(T item)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.Contains(item);
            }

            finally
            {
                LockList.ExitReadLock();
            }
        }

        // Contains
        #endregion

        #region ConvertAll

        /// <summary>
        /// Converts the elements of the threaded list to another type, and returns a list of the new type
        /// </summary>
        /// <typeparam name="TOutput">the destination type</typeparam>
        /// <param name="converter">delegate to convert the items to a new type</param>
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.ConvertAll(converter);
            }

            finally
            {
                LockList.ExitReadLock();
            }
        }

        // ConvertAll
        #endregion

        #region CopyTo

        /// <summary>
        /// Copies the elements of this threaded list to a one-dimension array of the same type
        /// </summary>
        /// <param name="array">the destination array</param>
        /// <param name="arrayIndex">index at which copying begins</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            LockList.EnterReadLock();
            try
            {
                m_TList.CopyTo(array, arrayIndex);
            }

            finally
            {
                LockList.ExitReadLock();
            }
        }

        // CopyTo
        #endregion

        #region Count

        /// <summary>
        /// Returns a count of the number of elements in this collection
        /// </summary>
        public int Count
        {
            get
            {
                LockList.EnterReadLock();
                try
                {
                    return m_TList.Count;
                }

                finally
                {
                    LockList.ExitReadLock();
                }
            }
        }

        // Count
        #endregion

        #region Exists

        /// <summary>
        /// Determines whether an item exists which meets the match criteria
        /// </summary>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public bool Exists(Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.Exists(match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // Exists
        #endregion

        #region Find

        /// <summary>
        /// Searches for an element that matches the criteria
        /// </summary>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public T Find(Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.Find(match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // Find
        #endregion

        #region FindAll

        /// <summary>
        /// Searches for elements that match the criteria
        /// </summary>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public List<T> FindAll(Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindAll(match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // FindAll
        #endregion

        #region FindIndex

        /// <summary>
        /// Returns the index of the element which matches the criteria
        /// </summary>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public int FindIndex(Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindIndex(match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the index of the element which matches the criteria
        /// </summary>
        /// <param name="startIndex">the zero-based index starting the search</param>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindIndex(startIndex, match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the index of the element which matches the criteria
        /// </summary>
        /// <param name="startIndex">the zero-based index starting the search</param>
        /// <param name="count">the number of elements to search</param>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindIndex(startIndex, count, match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // FindIndex
        #endregion

        #region FindLast

        /// <summary>
        /// Searches for the last element in the collection that matches the criteria
        /// </summary>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public T FindLast(Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindLast(match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // FindLast
        #endregion

        #region FindLastIndex

        /// <summary>
        /// Returns the last index of the element which matches the criteria
        /// </summary>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public int FindLastIndex(Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindLastIndex(match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the last index of the element which matches the criteria
        /// </summary>
        /// <param name="startIndex">the zero-based index starting the search</param>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindLastIndex(startIndex, match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the last index of the element which matches the criteria
        /// </summary>
        /// <param name="startIndex">the zero-based index starting the search</param>
        /// <param name="count">the number of elements to search</param>
        /// <param name="match">delegate that defines the conditions to search for</param>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.FindLastIndex(startIndex, count, match);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        // FindLastIndex
        #endregion

        #region ForEach

        /// <summary>
        /// Peforms the action on each element of the list
        /// </summary>
        /// <param name="action">the action to perfom</param>
        public void ForEach(Action<T> action)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.ForEach(action);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        // ForEach
        #endregion

        #region

        public void Truncate(Func<T, bool> predicate = null, bool distinct = false)
        {
            LockList.EnterWriteLock();
            try
            {
                if (predicate == null)
                {
                    if (distinct)
                    {
                        var a = m_TList.Distinct();
                        m_TList.Clear();
                        m_TList.AddRange(a);
                    }
                }
                else
                {
                    if (distinct)
                    {
                        var a = m_TList.Where(predicate);
                        m_TList.Clear();
                        m_TList.AddRange(a);
                    }
                    else
                    {
                        var a = m_TList.Where(predicate).Distinct();
                        m_TList.Clear();
                        m_TList.AddRange(a);
                    }
                }
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        public T[] Where(Func<T, bool> predicate, bool distinct = false, int take = 0)
        {
            LockList.EnterReadLock();
            try
            {
                if (distinct)
                {
                    if (take > 0)
                        return m_TList.Where(predicate).Take(take).ToArray();
                    else
                        return m_TList.Where(predicate).ToArray();
                }
                else
                {
                    if (take > 0)
                        return m_TList.Where(predicate).Distinct().Take(take).ToArray();
                    else
                        return m_TList.Where(predicate).Distinct().ToArray();
                }
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        public T[] Take(int take)
        {
            LockList.EnterReadLock();
            try
            {
                if (take > 0)
                    return m_TList.Take(take).ToArray();
            }
            finally
            {
                LockList.ExitReadLock();
            }
            return new T[] { };
        }

        public T[] Slice(int number)
        {
            LockList.EnterReadLock();
            try
            {
                if (number > 0)
                {
                    if (number < m_TList.Count)
                    {
                        T[] a = m_TList.Take(number).ToArray();
                        //m_TList = m_TList.Where(x => !a.Any(o => o.Equals(x))).ToList();
                        m_TList.RemoveRange(0, number);
                    }
                    else
                        return m_TList.ToArray();
                }
            }
            finally
            {
                LockList.ExitReadLock();
            }
            return new T[] { };
        }

        public bool Contain(T key)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.IndexOf(key) != -1;
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        #endregion

        /// <summary>
        /// Creates a shallow copy of the range of elements in the source
        /// </summary>
        /// <param name="index">index to start from</param>
        /// <param name="count">number of elements to return</param>
        /// <returns></returns>
        public List<T> GetRange(int index, int count)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.GetRange(index, count);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Searches the list and returns the index of the item found in the list
        /// </summary>
        /// <param name="item">the item to find</param>
        public int IndexOf(T item)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.IndexOf(item);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Searches the list and returns the index of the item found in the list
        /// </summary>
        /// <param name="item">the item to find</param>
        /// <param name="index">the zero-based index to begin searching from</param>
        public int IndexOf(T item, int index)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.IndexOf(item, index);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Searches the list and returns the index of the item found in the list
        /// </summary>
        /// <param name="item">the item to find</param>
        /// <param name="index">the zero-based index to begin searching from</param>
        /// <param name="count">the number of elements to search</param>
        public int IndexOf(T item, int index, int count)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.IndexOf(item, index, count);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Inserts the item into the list
        /// </summary>
        /// <param name="index">the index at which to insert the item</param>
        /// <param name="item">the item to insert</param>
        public void Insert(int index, T item)
        {
            LockList.ExitWriteLock();
            try
            {
                m_TList.Insert(index, item);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Insert a range of objects into the list
        /// </summary>
        /// <param name="index">index to insert at</param>
        /// <param name="range">range of values to insert</param>
        public void InsertRange(int index, IEnumerable<T> range)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.InsertRange(index, range);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Always false
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Returns the last index of the item in the list
        /// </summary>
        /// <param name="item">the item to find</param>
        public int LastIndexOf(T item)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.LastIndexOf(item);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the last index of the item in the list
        /// </summary>
        /// <param name="item">the item to find</param>
        /// <param name="index">the index at which to start searching</param>
        public int LastIndexOf(T item, int index)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.LastIndexOf(item, index);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the last index of the item in the list
        /// </summary>
        /// <param name="item">the item to find</param>
        /// <param name="index">the index at which to start searching</param>
        /// <param name="count">number of elements to search</param>
        public int LastIndexOf(T item, int index, int count)
        {
            LockList.EnterReadLock();
            try
            {
                return m_TList.LastIndexOf(item, index, count);
            }
            finally
            {
                LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes this item from the list
        /// </summary>
        /// <param name="item">the item to remove</param>
        public bool Remove(T item)
        {
            LockList.EnterWriteLock();
            try
            {
                return m_TList.Remove(item);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all the matching items from the list
        /// </summary>
        /// <param name="match">the pattern to search on</param>
        public int RemoveAll(Predicate<T> match)
        {
            LockList.EnterWriteLock();
            try
            {
                return m_TList.RemoveAll(match);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the item at the specified index
        /// </summary>
        /// <param name="index">the index of the item to remove</param>
        public void RemoveAt(int index)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.RemoveAt(index);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the items from the list
        /// </summary>
        /// <param name="index">the index of the item to begin removing</param>
        /// <param name="count">the number of items to remove</param>
        public void RemoveRange(int index, int count)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.RemoveRange(index, count);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Reverses the order of elements in the list
        /// </summary>
        public void Reverse()
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Reverse();
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Reverses the order of elements in the list
        /// </summary>
        /// <param name="index">the index to begin reversing at</param>
        /// <param name="count">the number of elements to reverse</param>
        public void Reverse(int index, int count)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Reverse(index, count);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Sorts the items in the list
        /// </summary>
        public void Sort()
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Sort();
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Sorts the items in the list
        /// </summary>
        /// <param name="comparison">the comparison to use when comparing elements</param>
        public void Sort(Comparison<T> comparison)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Sort(comparison);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Sorts the items in the list
        /// </summary>
        /// <param name="comparer">the comparer to use when comparing elements</param>
        public void Sort(IComparer<T> comparer)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Sort(comparer);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Sorts the items in the list
        /// </summary>
        /// <param name="index">the index to begin sorting at</param>
        /// <param name="count">the number of elements to sort</param>
        /// <param name="comparer">the comparer to use when sorting</param>
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.Sort(index, count, comparer);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Copies the elements of the list to an array
        /// </summary>
        public T[] ToArray(bool clearAfterThat = false)
        {
            if (clearAfterThat)
                LockList.EnterWriteLock();
            else
                LockList.EnterReadLock();
            try
            {
                T[] a = m_TList.ToArray();
                if (clearAfterThat) m_TList.Clear();
                return a;
            }
            finally
            {
                if (clearAfterThat)
                    LockList.ExitWriteLock();
                else
                    LockList.ExitReadLock();
            }
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the list, if that 
        /// number is less than the threshold
        /// </summary>
        public void TrimExcess()
        {
            LockList.EnterWriteLock();
            try
            {
                m_TList.TrimExcess();
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Determines whether all members of the list matches the conditions in the predicate
        /// </summary>
        /// <param name="match">the delegate which defines the conditions for the search</param>
        public bool TrueForAll(Predicate<T> match)
        {
            LockList.EnterWriteLock();
            try
            {
                return m_TList.TrueForAll(match);
            }
            finally
            {
                LockList.ExitWriteLock();
            }
        }

        #region IList<T> Members

        /// <summary>
        /// An item in the list
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                LockList.EnterReadLock();
                try
                {
                    return m_TList[index];
                }

                finally
                {
                    LockList.ExitReadLock();
                }
            }
            set
            {
                LockList.EnterWriteLock();
                try
                {
                    m_TList[index] = value;
                }

                finally
                {
                    LockList.ExitWriteLock();
                }
            }
        }

        #endregion
    }
}
