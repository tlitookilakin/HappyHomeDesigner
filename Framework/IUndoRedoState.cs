using System;

namespace HappyHomeDesigner.Framework
{
	public interface IUndoRedoState<T> : IEquatable<T> where T : IUndoRedoState<T>
	{
		public bool Apply(bool forward);
	}
}
