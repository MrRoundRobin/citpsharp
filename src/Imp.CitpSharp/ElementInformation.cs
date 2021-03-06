﻿using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///		Abstract class implementing properties shared by all element types
	/// </summary>
	[PublicAPI]
	public abstract class ElementInformation : IComparable<ElementInformation>
	{
		/// <summary>
		///		Base constructor for <see cref="ElementInformation"/>
		/// </summary>
		/// <param name="kind">Kind of this element</param>
		/// <param name="elementNumber">Unique number (in library) of this element</param>
		/// <param name="dmxRangeMin">Minimum value of the range which can be used to select this element when controlling from DMX</param>
		/// <param name="dmxRangeMax">Maximum value of the range which can be used to select this element when controlling from DMX</param>
		/// <param name="name">Name of this element</param>
		/// <param name="serialNumber">Unique serial number of this element</param>
		protected ElementInformation(ElementKind kind, byte elementNumber, byte dmxRangeMin,
			byte dmxRangeMax, [NotNull] string name, uint serialNumber)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Kind = kind;
			ElementNumber = elementNumber;
			SerialNumber = serialNumber;
			DmxRangeMin = dmxRangeMin;
			DmxRangeMax = dmxRangeMax;
			Name = name;
		}

		/// <summary>
		///		Kind of this element
		/// </summary>
		protected ElementKind Kind { get; }

		/// <summary>
		///		Unique number (in library) of this element
		/// </summary>
		public byte ElementNumber { get; }

		/// <summary>
		///		Unique serial number of this element
		/// </summary>
		public uint SerialNumber { get; }

		/// <summary>
		///		Minimum value of the range which can be used to select this element when controlling from DMX
		/// </summary>
		public byte DmxRangeMin { get; }

		/// <summary>
		///		Maximum value of the range which can be used to select this element when controlling from DMX
		/// </summary>
		public byte DmxRangeMax { get; }

		/// <summary>
		///		Name of this element
		/// </summary>
		public string Name { get; }

		/// <summary>
		///		Implementation of <see cref="IComparable"/> to order elements based upon <see cref="Kind"/> and <see cref="ElementNumber"/> properties
		/// </summary>
		/// <param name="other">Another instance of <see cref="ElementInformation"/></param>
		/// <returns>Order compared to <see cref="other"/></returns>
		public int CompareTo([CanBeNull] ElementInformation other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			return Kind == other.Kind ? ElementNumber.CompareTo(other.ElementNumber) : Kind.CompareTo(other.Kind);
		}

		/// <summary>
		///		Returns unique hashcode for this element
		/// </summary>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)Kind;
				hashCode = (hashCode * 397) ^ ElementNumber.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)SerialNumber;
				hashCode = (hashCode * 397) ^ DmxRangeMin.GetHashCode();
				hashCode = (hashCode * 397) ^ DmxRangeMax.GetHashCode();
				hashCode = (hashCode * 397) ^ Name.GetHashCode();
				return hashCode;
			}
		}

		/// <summary>
		///		Base implementation of equality comparison by value
		/// </summary>
		/// <param name="other">Another instance of <see cref="ElementInformation"/></param>
		/// <returns>True if equal, else false</returns>
		protected bool Equals([CanBeNull] ElementInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Kind == other.Kind && ElementNumber == other.ElementNumber && SerialNumber == other.SerialNumber
			       && DmxRangeMin == other.DmxRangeMin && DmxRangeMax == other.DmxRangeMax && string.Equals(Name, other.Name);
		}

		internal abstract void Serialize(CitpBinaryWriter writer, MsexVersion version);


		/// <summary>
		///		Kind of <see cref="ElementInformation"/>
		/// </summary>
		protected enum ElementKind
		{
			/// <summary>
			///		Instance is <see cref="MediaInformation"/>
			/// </summary>
			Media,
			/// <summary>
			///		Instance is <see cref="EffectInformation"/>
			/// </summary>
			Effect,
			/// <summary>
			///		Instance is <see cref="GenericInformation"/>
			/// </summary>
			Generic
		}
	}



	/// <summary>
	///		Information on a media element in an MSEX <see cref="ElementLibrary"/>
	/// </summary>
	[PublicAPI]
	public sealed class MediaInformation : ElementInformation, IEquatable<MediaInformation>
	{
		public MediaInformation(byte elementNumber, byte dmxRangeMin, byte dmxRangeMax,
			[NotNull] string name, DateTime mediaVersionTimestamp, ushort mediaWidth, ushort mediaHeight, uint mediaLength,
			byte mediaFps, uint serialNumber)
			: base(ElementKind.Media, elementNumber, dmxRangeMin, dmxRangeMax, name, serialNumber)
		{
			MediaVersionTimestamp = mediaVersionTimestamp;
			MediaWidth = mediaWidth;
			MediaHeight = mediaHeight;
			MediaLength = mediaLength;
			MediaFps = mediaFps;
		}


		public DateTime MediaVersionTimestamp { get; }
		public ushort MediaWidth { get; }
		public ushort MediaHeight { get; }
		public uint MediaLength { get; }
		public byte MediaFps { get; }


		public bool Equals([CanBeNull] MediaInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return MediaVersionTimestamp.Equals(other.MediaVersionTimestamp) && MediaWidth == other.MediaWidth
			       && MediaHeight == other.MediaHeight && MediaLength == other.MediaLength && MediaFps == other.MediaFps;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is MediaInformation && Equals((MediaInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = base.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaVersionTimestamp.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaWidth.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaHeight.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)MediaLength;
				hashCode = (hashCode * 397) ^ MediaFps.GetHashCode();
				return hashCode;
			}
		}

		internal static MediaInformation Deserialize(CitpBinaryReader reader, MsexVersion version)
		{
			byte elementNumber = reader.ReadByte();

			uint serialNumber = 0;
			if (version == MsexVersion.Version1_2)
				serialNumber = reader.ReadUInt32();

			byte dmxRangeMin = reader.ReadByte();
			byte dmxRangeMax = reader.ReadByte();
			string name = reader.ReadString();
			var mediaVersionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64());
			ushort mediaWidth = reader.ReadUInt16();
			ushort mediaHeight = reader.ReadUInt16();
			uint mediaLength = reader.ReadUInt32();
			byte mediaFps = reader.ReadByte();

			return new MediaInformation(elementNumber, dmxRangeMin, dmxRangeMax, name, mediaVersionTimestamp, mediaWidth,
				mediaHeight, mediaLength, mediaFps, serialNumber);
		}

		internal override void Serialize(CitpBinaryWriter writer, MsexVersion version)
		{
			writer.Write(ElementNumber);

			if (version == MsexVersion.Version1_2)
				writer.Write(SerialNumber);

			writer.Write(DmxRangeMin);
			writer.Write(DmxRangeMax);
			writer.Write(Name);
			writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(MediaVersionTimestamp));
			writer.Write(MediaWidth);
			writer.Write(MediaHeight);
			writer.Write(MediaLength);
			writer.Write(MediaFps);
		}
	}


	/// <summary>
	///		Information on an effect element in an MSEX <see cref="ElementLibrary"/>
	/// </summary>
	[PublicAPI]
	public sealed class EffectInformation : ElementInformation, IEquatable<EffectInformation>
	{
		public EffectInformation(byte elementNumber, byte dmxRangeMin, byte dmxRangeMax, [NotNull] string name,
			[NotNull] ImmutableList<string> effectParameterNames, uint serialNumber)
			: base(ElementKind.Effect, elementNumber, dmxRangeMin, dmxRangeMax, name, serialNumber)
		{
			if (effectParameterNames == null)
				throw new ArgumentNullException(nameof(effectParameterNames));

			if (effectParameterNames.Any(n => n == null))
				throw new ArgumentNullException(nameof(effectParameterNames), "Cannot contain null items");

			EffectParameterNames = effectParameterNames;
		}

		public ImmutableList<string> EffectParameterNames { get; }

		public bool Equals([CanBeNull] EffectInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			return ReferenceEquals(this, other) || EffectParameterNames.Equals(other.EffectParameterNames);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is EffectInformation && Equals((EffectInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ EffectParameterNames.GetHashCode();
			}
		}

		internal static EffectInformation Deserialize(CitpBinaryReader reader, MsexVersion version)
		{
			byte elementNumber = reader.ReadByte();

			uint serialNumber = 0;
			if (version == MsexVersion.Version1_2)
				serialNumber = reader.ReadUInt32();

			byte dmxRangeMin = reader.ReadByte();
			byte dmxRangeMax = reader.ReadByte();
			string name = reader.ReadString();
			var effectParameterNames = reader.ReadCollection(TypeCode.Byte, reader.ReadString).ToImmutableList();

			return new EffectInformation(elementNumber, dmxRangeMin, dmxRangeMax, name, effectParameterNames, serialNumber);
		}

		internal override void Serialize(CitpBinaryWriter writer, MsexVersion version)
		{
			writer.Write(ElementNumber);

			if (version == MsexVersion.Version1_2)
				writer.Write(SerialNumber);

			writer.Write(DmxRangeMin);
			writer.Write(DmxRangeMax);
			writer.Write(Name);
			writer.Write(EffectParameterNames, TypeCode.Byte, writer.Write);
		}
	}


	/// <summary>
	///		Information on a generic element in an MSEX <see cref="ElementLibrary"/>
	/// </summary>
	[PublicAPI]
	public sealed class GenericInformation : ElementInformation, IEquatable<GenericInformation>
	{
		public GenericInformation(byte elementNumber, byte dmxRangeMin, byte dmxRangeMax, [NotNull] string name,
			DateTime versionTimestamp, uint serialNumber)
			: base(ElementKind.Generic, elementNumber, dmxRangeMin, dmxRangeMax, name, serialNumber)
		{
			VersionTimestamp = versionTimestamp;
		}

		public DateTime VersionTimestamp { get; set; }

		public bool Equals([CanBeNull] GenericInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			return ReferenceEquals(this, other) || VersionTimestamp.Equals(other.VersionTimestamp);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is GenericInformation && Equals((GenericInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ VersionTimestamp.GetHashCode();
			}
		}

		internal static GenericInformation Deserialize(CitpBinaryReader reader, MsexVersion version)
		{
			byte elementNumber = reader.ReadByte();
			uint serialNumber = 0;

			if (version == MsexVersion.Version1_2)
				serialNumber = reader.ReadUInt32();

			byte dmxRangeMin = reader.ReadByte();
			byte dmxRangeMax = reader.ReadByte();
			string name = reader.ReadString();
			var versionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64());

			return new GenericInformation(elementNumber, dmxRangeMin, dmxRangeMax, name, versionTimestamp, serialNumber);
		}

		internal override void Serialize(CitpBinaryWriter writer, MsexVersion version)
		{
			writer.Write(ElementNumber);

			if (version == MsexVersion.Version1_2)
				writer.Write(SerialNumber);

			writer.Write(DmxRangeMin);
			writer.Write(DmxRangeMax);
			writer.Write(Name);
			writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(VersionTimestamp));
		}
	}
}