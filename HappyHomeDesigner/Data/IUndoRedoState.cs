using System;

namespace HappyHomeDesigner.Data
{
	public interface IUndoRedoState<T> : IEquatable<T> where T : IUndoRedoState<T>
	{
		public bool Apply(bool forward);
	}
}
