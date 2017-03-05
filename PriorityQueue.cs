using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncomaniaSolver
{
    /// <summary>
    /// Simple binary min-heap implementation
    /// Element at 0 is never used
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private T[] _items;
        private int _size;

        private static readonly T[] _emptyArray = new T[0];

        public PriorityQueue()
        {
            _items = new T[32];
        }

        public PriorityQueue( int capacity )
        {
            if ( capacity < 0 )
                throw new ArgumentOutOfRangeException();

            if ( capacity == 0 )
                _items = _emptyArray;
            else
                _items = new T[capacity];
        }

        public int Capacity
        {
            get
            {
                return _items.Length;
            }
            set
            {
                if ( value <= _size )
                    throw new ArgumentOutOfRangeException();

                if ( value != _items.Length )
                {
                    if ( value > 0 )
                    {
                        T[] newItems = new T[value];
                        if ( _size > 0 )
                            Array.Copy( _items, 1, newItems, 1, _size );

                        _items = newItems;
                    }
                    else
                    {
                        _items = _emptyArray;
                    }
                }
            }
        }

        public int Count
        {
            get { return _size; }
        }

        public void Add( T value )
        {
            if ( _items.Length - 1 == _size )
                Capacity = Capacity * 2;

            var pos = ++_size;

            for ( ; pos > 1 && value.CompareTo( _items[pos / 2] ) < 0; pos = pos / 2 )
                _items[pos] = _items[pos / 2];

            _items[pos] = value;
        }

        public T RemoveMin()
        {
            if ( _size == 0 )
                throw new InvalidOperationException();

            var value = _items[1];

            _items[1] = _items[_size--];

            Sink( 1 );

            return value;
        }

        private void Sink( int idx )
        {
            var value = _items[idx];
            
            for ( int child; idx * 2 <= _size; idx = child )
            {
                child = idx * 2;
                if ( child != _size && _items[child].CompareTo( _items[child + 1] ) > 0 )
                    child++;

                if ( _items[child].CompareTo( value ) < 0 )
                    _items[idx] = _items[child];
                else
                    break;
            }

            _items[idx] = value;
        }
    }
}
