using System;
using System.Collections;
using System.Collections.Generic;

namespace HappyHomeDesigner.Data
{
	public class ListSlice<T> : IList<T> where T : class
	{
		public IList<T> Source;
		public Range Range
		{
			get => range;
			set
			{
				range = value;
				UpdateRangeIfNeeded(true);
			}
		}

		private int Start;
		private int Length;
		private int SourceLen;
		private Range range;

		public ListSlice(IList<T> source, Range range)
		{
			Source = source;
			SourceLen = source.Count;
			Range = range;
		}

		public T this[int index]
		{
			get
			{
				UpdateRangeIfNeeded();
				if (index >= Length)
					throw new ArgumentOutOfRangeException(nameof(index));

				return Source[index + Start];
			}
			set
			{
				UpdateRangeIfNeeded();
				if (index >= Length)
					throw new ArgumentOutOfRangeException(nameof(index));

				Source[index + Start] = value;
			}
		}

		public int Count
		{
			get
			{
				UpdateRangeIfNeeded();
				return Length;
			}
		}

		public bool IsReadOnly => Source.IsReadOnly;

		public void Add(T item)
		{
			UpdateRangeIfNeeded();
			Source.Insert(Start + Length - 1, item);
		}

		private void UpdateRangeIfNeeded(bool force = false)
		{
			if (!force && Source.Count == SourceLen)
				return;

			SourceLen = Source.Count;
			Start = range.Start.GetOffset(SourceLen);
			var end = Math.Min(range.End.GetOffset(SourceLen), SourceLen);
			if (Start >= Source.Count)
				Start = Math.Max(Source.Count - 1, 0);
			Length = end < Start ? 0 : end - Start;
		}

		public void Clear()
		{
			Source.Clear();
		}

		public bool Contains(T item)
		{
			UpdateRangeIfNeeded();
			for (int i = 0; i < Length; i++)
				if (Source[i + Start] == item)
					return true;
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			UpdateRangeIfNeeded();
			Source.CopyTo(array, arrayIndex + Start);
		}

		public IEnumerator<T> GetEnumerator()
		{
			UpdateRangeIfNeeded();
			return new ClippedEnumerator(this);
		}

		public int IndexOf(T item)
		{
			UpdateRangeIfNeeded();
			for (int i = 0; i < Length; i++)
				if (Source[i + Start] == item)
					return i;
			return -1;
		}

		public void Insert(int index, T item)
		{
			UpdateRangeIfNeeded();
			if (index >= Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			Source.Insert(index + Start, item);
		}

		public bool Remove(T item)
		{
			UpdateRangeIfNeeded();
			for (int i = Length - 1; i >= 0; i--)
			{
				if (Source[i + Start] == item)
				{
					Source.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		public void RemoveAt(int index)
		{
			UpdateRangeIfNeeded();
			if (index >= Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			Source.RemoveAt(index + Start);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			UpdateRangeIfNeeded();
			return new ClippedEnumerator(this);
		}

		private class ClippedEnumerator(ListSlice<T> owner) : IEnumerator<T>
		{
			private readonly ListSlice<T> Owner = owner;

			public T Current => current;
			private T current = default;
			object IEnumerator.Current => current;
			private int index = -1;

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				index++;
				if (index < Owner.Length)
				{
					current = Owner.Source[index + Owner.Start];
					return true;
				}
				return false;
			}

			public void Reset()
			{
				index = -1;
				current = default;
			}
		}
	}
}
