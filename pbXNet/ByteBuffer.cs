using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;

namespace pbXNet
{
	public class ByteBuffer : IByteBuffer, IDisposable, IEnumerable<byte>
	{
		byte[] _b;

		public int Length => (_b == null ? 0 : _b.Length);

		public ByteBuffer()
		{
			_b = new byte[0];
		}

		public ByteBuffer(byte[] b, bool clearSource = false)
		{
			if (b == null)
				throw new ArgumentNullException(nameof(b));

			_b = (byte[])b.Clone();

			if (clearSource)
				ClearSource(b);
		}

		void ClearSource(byte[] b)
		{
#if !WINDOWS_UWP && !NETSTANDARD1_6
			if (!b.IsReadOnly)
#endif
			{
				b.FillWith<byte>(0);
				System.Array.Resize<byte>(ref b, 0);
			}
		}

		public ByteBuffer(IByteBuffer b, bool clearSource = false)
		{
			if (b == null)
				throw new ArgumentNullException(nameof(b));

			_b = (byte[])b.GetBytes().Clone();
			if (!clearSource)
				b.DisposeBytes();
			else
				b.Dispose();
		}

		public ByteBuffer(ByteBuffer b, bool clearSource = false)
		{
			if (b == null)
				throw new ArgumentNullException(nameof(b));

			_b = (byte[])b._b.Clone();
			if (clearSource)
				b.Dispose();
		}

		public ByteBuffer(string sb, Encoding encoding)
			: this(encoding.GetBytes(sb), true)
		{
		}

		public ByteBuffer(IEnumerable<byte> l)
			: this(l.ToArray(), true)
		{
		}

		public ByteBuffer(Stream s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			if (s is MemoryStream)
			{
				_b = (s as MemoryStream).ToArray();
			}
			else
			{
				using (MemoryStream sm = new MemoryStream())
				{
					if (s.CanSeek)
						s.Seek(0, SeekOrigin.Begin);
					s.CopyTo(sm);
					_b = sm.ToArray();
				}
			}
		}

		public virtual void Dispose()
		{
			_b?.FillWith<byte>(0);
			_b = null;
		}

		public ByteBuffer Append(byte[] b, bool clearSource = false)
		{
			if (b == null)
				throw new ArgumentNullException(nameof(b));

			int i = _b.Length;
			Array.Resize(ref _b, i + b.Length);
			Array.Copy(b, 0, _b, i, b.Length);

			if (clearSource)
				ClearSource(b);

			return this;
		}

		public ByteBuffer Append(IByteBuffer b, bool clearSource = false)
		{
			if (b == null)
				throw new ArgumentNullException(nameof(b));

			Append(b.GetBytes(), false);
			if (!clearSource)
				b.DisposeBytes();
			else
				b.Dispose();
			return this;
		}

		public ByteBuffer Append(ByteBuffer b, bool clearSource = false)
		{
			if (b == null)
				throw new ArgumentNullException(nameof(b));

			Append(b._b, false);
			if (clearSource)
				b.Dispose();
			return this;
		}

		public ByteBuffer Append(string sb, Encoding encoding) => Append(encoding.GetBytes(sb), true);

		public ByteBuffer Append(IEnumerable<byte> l) => Append(l.ToArray(), true);

		public ByteBuffer Append(Stream s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			if (s is MemoryStream)
				Append((s as MemoryStream).ToArray());
			else
			{
				using (MemoryStream sm = new MemoryStream())
				{
					if (s.CanSeek)
						s.Seek(0, SeekOrigin.Begin);
					s.CopyTo(sm);
					Append(sm.ToArray());
				}
			}
			return this;
		}

		// TODO: Insert, Delete

		public static ByteBuffer NewFromHexString(string d)
		{
			return new ByteBuffer(d.FromHexString());
		}

		public static ByteBuffer NewFromString(string d, Encoding encoding)
		{
			return new ByteBuffer(encoding.GetBytes(d));
		}

		public virtual byte[] GetBytes()
		{
			return _b;
		}

		public static implicit operator byte[] (ByteBuffer bb)
		{
			return bb?.GetBytes();
		}

		public virtual string ToHexString()
		{
			return _b?.ToHexString();
		}

		public virtual string ToString(Encoding encoding)
		{
			return encoding.GetString(_b);
		}

		public override string ToString()
		{
			return ToHexString();
		}

		public override bool Equals(object obj)
		{
			ByteBuffer p = obj as ByteBuffer;
			if (p == null)
				return false;
			return this.Equals(p);
		}

		public bool Equals(ByteBuffer b)
		{
			if (object.ReferenceEquals(b, null))
				return false;
			if (object.ReferenceEquals(this, b))
				return true;
			if (this.GetType() != b.GetType())
				return false;

			if (_b == null || b._b == null)
				return _b == null && b._b == null;
			
			return _b.SequenceEqual(b._b);
		}

		public override int GetHashCode()
		{
			return _b.GetHashCode();
		}

		public IEnumerator<byte> GetEnumerator()
		{
			return new ArrayExtensions.Enumerator<byte>(_b);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _b.GetEnumerator();
		}

		public virtual void DisposeBytes()
		{
		}
	}
}