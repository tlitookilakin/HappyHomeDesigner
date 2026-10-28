
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace HappyHomeDesigner.Framework
{
	public static class TimeChunker
	{
		public static IEnumerable<IEnumerable<T>> TimeChunk<T>(this IEnumerable<T> source, int millis)
		{
			return new ChunkEnumerator<T>(source, millis);
		}

		private class ChunkEnumerator<T>(IEnumerable<T> source, int millis) : IEnumerable<IEnumerable<T>>
		{
			public IEnumerator<IEnumerable<T>> GetEnumerator()
			{
				return new TimeChunk(source.GetEnumerator(), millis);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			private class TimeChunk(IEnumerator<T> source, int millis) : IEnumerator<IEnumerable<T>>
			{
				private readonly Stopwatch timer = new();
				private bool disposed = false;
				private readonly long ticks = millis * TimeSpan.TicksPerMillisecond;
				private bool EndOfSequence = false;

				public IEnumerable<T> Current { get; private set; }

				object IEnumerator.Current => Current;

				public void Dispose()
				{
					if (disposed)
						return;

					disposed = true;
					source.Dispose();
				}

				public bool MoveNext()
				{
					if (disposed || EndOfSequence)
						return false;

					Current = GetItems();
					return !EndOfSequence;
				}

				private IEnumerable<T> GetItems()
				{
					timer.Restart();
					while (source.MoveNext())
					{
						yield return source.Current;

						if (timer.ElapsedTicks > ticks)
							yield break;
					}
					EndOfSequence = true;
				}

				public void Reset()
				{
					source.Reset();
					EndOfSequence = false;
				}
			}
		}
	}
}
