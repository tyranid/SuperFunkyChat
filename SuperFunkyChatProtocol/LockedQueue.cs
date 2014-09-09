//    SuperFunkyChat - Example Binary Network Application
//    Copyright (C) 2014 James Forshaw
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading;

namespace SuperFunkyChatProtocol
{
    /// <summary>
    /// Generic queue which can be used in a multi-threaded app and waits for a queue value
    /// </summary>
    /// <typeparam name="T">Type of object to queue, must be a reference type</typeparam>
    public class LockedQueue<T> : IDisposable where T : class 
    {
        /// <summary>
        /// Internal data queue
        /// </summary>
        private Queue<T> _dataQueue;
        /// <summary>
        /// Event wait handle to indicate data is available in the queue
        /// </summary>
        private EventWaitHandle _readyEvent;
        /// <summary>
        /// Indicates this queue is stopped
        /// </summary>
        private bool _stopped;

        /// <summary>
        /// Internal initialization
        /// </summary>
        private void Initialize()
        {
            _readyEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~LockedQueue()
        {
            _readyEvent.Close();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public LockedQueue()            
        {
            _dataQueue = new Queue<T>();
            Initialize();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ienum">Initialization data for queue</param>
        public LockedQueue(IEnumerable<T> ienum)            
        {
            _dataQueue = new Queue<T>(ienum);
            Initialize();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="size">Specify the size of the queue</param>
        public LockedQueue(int size)            
        {
            _dataQueue = new Queue<T>(size);
            Initialize();
        }

        /// <summary>
        /// Enqueue a new item (thread safe)
        /// </summary>
        /// <param name="item">The item to queue</param>
        public void Enqueue(T item)
        {
            lock (_dataQueue)
            {
                _dataQueue.Enqueue(item);
                _readyEvent.Set();
            }
        }

        /// <summary>
        /// Dequeue an item, waiting for a specified time
        /// </summary>        
        /// <returns>The item (null will be returned if the queue has been stopped)</returns>
        public T Dequeue()
        {
            T ret = null;
            bool bDequeue = false;

            while (!bDequeue && !_stopped)
            {                                
                if (_readyEvent.WaitOne())
                {
                    if (!_stopped)
                    {
                        lock (_dataQueue)
                        {
                            if (_dataQueue.Count > 0)
                            {
                                ret = _dataQueue.Dequeue();
                                bDequeue = true;
                            }

                            // If we run out of data, then reset event flag
                            if (_dataQueue.Count == 0)
                            {
                                if (!_stopped)
                                {
                                    _readyEvent.Reset();
                                }
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            return ret;
        }        

        /// <summary>
        /// Gets the count of items in the queue
        /// </summary>
        public int Count { 
            get {
                int count;

                lock (_dataQueue)
                {
                    count = _dataQueue.Count;
                }

                return count;
            } 
        }

        /// <summary>
        /// Stop the queue and try and unlock all waiting threads
        /// </summary>
        public void Stop()
        {
            if (!_stopped)
            {
                // Set stop and queue up a null and set the event to hopefully allow anyone to exit
                _stopped = true;
                _readyEvent.Set();
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            _readyEvent.Dispose();
        }

        #endregion
    }
}
