using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    /// Thread safe generic dictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictionaryThreadSafe<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Variables

        /// <summary>
        /// Lock for the dictionary
        /// </summary>
        private readonly ReaderWriterLockSlim Lock_Dictionary = new ReaderWriterLockSlim();

        /// <summary>
        /// The base dictionary
        /// </summary>
        //private readonly Dictionary<TKey, TValue> m_Dictionary;
        private Dictionary<TKey, TValue> m_Dictionary;
        //private readonly BoisSerializer boisSerializer = new BoisSerializer();

        // Variables
        #endregion

        #region Init

        /// <summary>
        /// Initializes the dictionary object
        /// </summary>
        public DictionaryThreadSafe()
        {
            m_Dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes the dictionary object
        /// </summary>
        /// <param name="capacity">initial capacity of the dictionary</param>
        public DictionaryThreadSafe(int capacity)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        /// <summary>
        /// Initializes the dictionary object
        /// </summary>
        /// <param name="comparer">the comparer to use when comparing keys</param>
        public DictionaryThreadSafe(IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Initializes the dictionary object
        /// </summary>
        /// <param name="dictionary">the dictionary whose keys and values are copied to this object</param>
        public DictionaryThreadSafe(IDictionary<TKey, TValue> dictionary)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Initializes the dictionary object
        /// </summary>
        /// <param name="capacity">initial capacity of the dictionary</param>
        /// <param name="comparer">the comparer to use when comparing keys</param>
        public DictionaryThreadSafe(int capacity, IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        /// <summary>
        /// Initializes the dictionary object
        /// </summary>
        /// <param name="dictionary">the dictionary whose keys and values are copied to this object</param>
        /// <param name="comparer">the comparer to use when comparing keys</param>
        public DictionaryThreadSafe(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        // Init
        #endregion

        #region IDictionary<TKey,TValue> Members

        #region GetValueAddIfNotExist

        /// <summary>
        /// Returns the value of <paramref name="key"/>. If <paramref name="key"/>
        /// does not exist, <paramref name="func"/> is performed and added to the 
        /// dictionary
        /// </summary>
        /// <param name="key">the key to check</param>
        /// <param name="func">the delegate to call if key does not exist</param>
        public TValue GetValueAddIfNotExist(TKey key, Func<TValue> func)
        {
            // enter a write lock, to make absolutely sure that the key
            // is added/deleted from the time we check if it exists
            // to the time we add it if it doesn't exist
            return Lock_Dictionary.PerformUsingUpgradeableReadLock(() =>
            {
                TValue rVal;

                // if we have the value, get it and exit
                if (m_Dictionary.TryGetValue(key, out rVal))
                    return rVal;

                // not found, so do the function to get the value
                Lock_Dictionary.PerformUsingWriteLock(() =>
                {
                    rVal = func.Invoke();

                    // add to the dictionary
                    m_Dictionary.Add(key, rVal);

                    // return the value
                    return rVal;
                });

                return rVal;
            });
        }

        // GetValueAddIfNotExist
        #endregion

        #region Add

        /// <summary>
        /// Adds an item to the dictionary
        /// </summary>
        /// <param name="key">the key to add</param>
        /// <param name="value">the value to add</param>
        public void Add(TKey key, TValue value)
        {
            Lock_Dictionary.PerformUsingWriteLock(() => m_Dictionary.Add(key, value));
        }
        public void ReadFile(string file_path)
        {
            //Lock_Dictionary.PerformUsingWriteLock(() =>
            //{
            //    if (File.Exists(file_path))
            //    {
            //        using (var file = File.OpenRead(file_path))
            //        {
            //            //ProtoBuf.Serializer.Serialize<Dictionary<string, string>>(file, cacheData); 
            //            file.Position = 0;
            //            m_Dictionary = boisSerializer.Deserialize<Dictionary<TKey, TValue>>(file); 
            //            file.Close();
            //        }
            //    }
            //});
        }

        public void WriteFile(string file_path)
        {
            //Lock_Dictionary.PerformUsingWriteLock(() =>
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

            //            boisSerializer.Serialize<Dictionary<TKey, TValue>>(m_Dictionary, file);
            //            file.SetLength(file.Position);
            //            file.Close();
            //        }
            //    }
            //    else
            //    {
            //        using (var file = new FileStream(file_path, FileMode.OpenOrCreate))
            //        {
            //            //ProtoBuf.Serializer.Serialize<Dictionary<string, string>>(file, cacheData); 
            //            boisSerializer.Serialize<Dictionary<TKey, TValue>>(m_Dictionary, file);
            //            file.Close();
            //        }
            //    }
            //});
        }

        /// <summary>
        /// Adds an item to the dictionary
        /// </summary>
        /// <param name="item">the key / values to add</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            TKey key = item.Key;
            TValue value = item.Value;
            Lock_Dictionary.PerformUsingWriteLock(() => m_Dictionary.Add(key, value));
        }

        // Add
        #endregion

        #region AddIfNotExists

        /// <summary>
        /// Adds the value if it's key does not already exist. Returns
        /// true if the value was added
        /// </summary>
        /// <param name="key">the key to check, add</param>
        /// <param name="value">the value to add if the key does not already exist</param>
        public bool AddIfNotExists(TKey key, TValue value)
        {
            bool rVal = false;

            Lock_Dictionary.PerformUsingWriteLock(() =>
            {
                // if not exist, then add it
                if (!m_Dictionary.ContainsKey(key))
                {
                    // add the value and set the flag
                    m_Dictionary.Add(key, value);
                    rVal = true;
                }
            });

            return rVal;
        }

        /// <summary>
        /// Adds the list of value if the keys do not already exist.
        /// </summary>
        /// <param name="keys">the keys to check, add</param>
        /// <param name="defaultValue">the value to add if the key does not already exist</param>
        public void AddIfNotExists(IEnumerable<TKey> keys, TValue defaultValue)
        {
            Lock_Dictionary.PerformUsingWriteLock(() =>
            {
                foreach (TKey key in keys)
                {
                    // if not exist, then add it
                    if (!m_Dictionary.ContainsKey(key))
                        m_Dictionary.Add(key, defaultValue);
                }
            });
        }

        // AddIfNotExists
        #endregion

        #region AddIfNotExistsElseUpdate

        /// <summary>
        /// Adds the value if it's key does not already exist. Returns
        /// true if the value was added. 
        /// If the key already exists, it's value is updated and returns false.
        /// </summary>
        /// <param name="key">the key to check, add</param>
        /// <param name="value">the value to add if the key does not already exist</param>
        public bool AddIfNotExistsElseUpdate(TKey key, TValue value)
        {
            bool rVal = false;

            Lock_Dictionary.PerformUsingWriteLock(() =>
            {
                // if not exist, then add it
                if (!m_Dictionary.ContainsKey(key))
                {
                    // add the value and set the flag
                    m_Dictionary.Add(key, value);
                    rVal = true;
                }
                else
                    m_Dictionary[key] = value;
            });

            return rVal;
        }

        // AddIfNotExistsElseUpdate
        #endregion

        #region UpdateValueIfKeyExists

        /// <summary>
        /// Updates the value of the key if the key exists. Returns true if updated
        /// </summary>
        /// <param name="key"></param>
        /// <param name="NewValue"></param>
        public bool UpdateValueIfKeyExists(TKey key, TValue NewValue)
        {
            bool rVal = false;

            Lock_Dictionary.PerformUsingWriteLock(() =>
            {
                // if we have the key, then update it
                if (m_Dictionary.ContainsKey(key))
                {
                    m_Dictionary[key] = NewValue;
                    rVal = true;
                }
            });

            return rVal;
        }

        // UpdateValueIfKeyExists
        #endregion

        #region Contains

        /// <summary>
        /// Returns true if the key value pair exists in the dictionary
        /// </summary>
        /// <param name="item">key value pair to find</param>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Lock_Dictionary.PerformUsingReadLock(() => ((m_Dictionary.ContainsKey(item.Key)) &&
                                                               (m_Dictionary.ContainsValue(item.Value))));
        }

        // Contains
        #endregion

        #region ContainsKey

        /// <summary>
        /// Returns true if the key exists in the dictionary
        /// </summary>
        /// <param name="key">the key to find in the dictionary</param>
        public bool ContainsKey(TKey key)
        {
            return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.ContainsKey(key));
        }

        // ContainsKey
        #endregion

        #region ContainsValue

        /// <summary>
        /// Returns true if the dictionary contains this value
        /// </summary>
        /// <param name="value">the value to find</param>
        public bool ContainsValue(TValue value)
        {
            return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.ContainsValue(value));
        }

        // ContainsValue
        #endregion

        #region Keys

        /// <summary>
        /// Returns the keys as a collection
        /// </summary>
        public ICollection<TKey> Keys
        {
            get { return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.Keys); }
        }

        /// <summary>
        /// Returns the keys as a collection
        /// </summary>
        public TKey[] KeysArray
        {
            get { return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.Keys.ToArray()); }
        }

        // Keys
        #endregion

        #region Remove

        /// <summary>
        /// Removes the element with this key name
        /// </summary>
        /// <param name="key">the key to remove</param>
        public bool Remove(TKey key)
        {
            return Lock_Dictionary.PerformUsingWriteLock(() => (!m_Dictionary.ContainsKey(key)) || m_Dictionary.Remove(key));
        }

        /// <summary>
        /// Removes the element with this key name and value. Returns
        /// true if the item was removed.
        /// </summary>
        /// <param name="item">the key to remove</param>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Lock_Dictionary.PerformUsingWriteLock(() =>
            {
                // skip if the key doesn't exist
                TValue tempVal;
                if (!m_Dictionary.TryGetValue(item.Key, out tempVal))
                    return false;

                // skip if the value's don't match
                if (!tempVal.Equals(item.Value))
                    return false;

                return m_Dictionary.Remove(item.Key);
            });
        }

        /// <summary>
        /// Removes items from the dictionary that match a pattern. Returns true
        /// on success
        /// </summary>
        /// <param name="predKey">Optional expression based on the keys</param>
        /// <param name="predValue">Option expression based on the values</param>
        public bool Remove(Predicate<TKey> predKey, Predicate<TValue> predValue)
        {
            return Lock_Dictionary.PerformUsingWriteLock(() =>
            {
                // exit if no keys
                if (m_Dictionary.Keys.Count == 0)
                    return true;

                // holds the list of items to be deleted
                List<TKey> DeleteList = new List<TKey>();

                // process keys
                foreach (TKey key in m_Dictionary.Keys)
                {
                    bool IsMatch = false;

                    // add the item to the list if it matches the predicate
                    if (predKey != null)
                        IsMatch = (predKey(key));

                    // if this item's value matches, add it
                    if ((!IsMatch) && (predValue != null) && (predValue(m_Dictionary[key])))
                        IsMatch = true;

                    // add to the list if we have a match
                    if (IsMatch)
                        DeleteList.Add(key);
                }

                // delete all the items from the list
                foreach (TKey item in DeleteList)
                    m_Dictionary.Remove(item);

                return true;
            });
        }

        // Remove
        #endregion

        #region TryGetValue

        /// <summary>
        /// Attemtps to return the value found at element <paramref name="key"/>
        /// If no value is found, returns false
        /// </summary>
        /// <param name="key">the key to find</param>
        /// <param name="value">the value returned if the key is found</param>
        public bool TryGetValue(TKey key, out TValue value)
        {
            Lock_Dictionary.EnterReadLock();
            try
            {
                return m_Dictionary.TryGetValue(key, out value);
            }
            finally
            {
                Lock_Dictionary.ExitReadLock();
            }

        }

        public TValue[] GetValues(TKey[] keys)
        {
            Lock_Dictionary.EnterReadLock();
            try
            {
                List<TValue> ls = new List<TValue>();
                if (keys.Length > 0) {
                    for (int i = 0; i < keys.Length; i++) {
                        if (m_Dictionary.ContainsKey(keys[i]))
                            ls.Add(m_Dictionary[keys[i]]);
                    }
                }
                return ls.ToArray();
            }
            finally
            {
                Lock_Dictionary.ExitReadLock();
            }
        }

        // TryGetValue
        #endregion

        #region Values

        /// <summary>
        /// Returns a collection of the values in the dictionary
        /// </summary>
        public ICollection<TValue> Values
        {
            get { return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.Values); }
        }

        /// <summary>
        /// Returns a collection of the values in the dictionary
        /// </summary>
        public TValue[] ValuesArray
        {
            get { return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.Values.ToArray()); }
        }

        // Values
        #endregion

        #region this

        /// <summary>
        /// Value 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get { return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary[key]); }

            set { Lock_Dictionary.PerformUsingWriteLock(() => m_Dictionary[key] = value); }
        }

        // this
        #endregion

        #region Clear

        /// <summary>
        /// Clears the dictionary
        /// </summary>
        public void Clear()
        {
            Lock_Dictionary.PerformUsingWriteLock(() => m_Dictionary.Clear());
        }

        // Clear
        #endregion

        #region CopyTo

        /// <summary>
        /// Copies the items of the dictionary to a key value pair array
        /// </summary>
        /// <param name="array">the key value pair collection to copy into</param>
        /// <param name="arrayIndex">the index to begin copying to</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.ToArray().CopyTo(array, arrayIndex));
        }

        // CopyTo
        #endregion

        #region Count

        /// <summary>
        /// Returns the number of items in the dictionary
        /// </summary>
        public int Count
        {
            get { return Lock_Dictionary.PerformUsingReadLock(() => m_Dictionary.Count); }
        }

        // Count
        #endregion

        #region IsReadOnly

        /// <summary>
        /// Always returns false
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        // IsReadOnly
        #endregion

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Dictionary<TKey, TValue> localDict = null;

            Lock_Dictionary.PerformUsingReadLock(() => localDict = new Dictionary<TKey, TValue>(m_Dictionary));

            // get the enumerator
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)localDict).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// The get enumerator.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            Dictionary<TKey, TValue> localDict = null;

            Lock_Dictionary.PerformUsingReadLock(() => localDict = new Dictionary<TKey, TValue>(m_Dictionary));

            // get the enumerator
            return localDict.GetEnumerator();
        }

        #endregion
    }
}
