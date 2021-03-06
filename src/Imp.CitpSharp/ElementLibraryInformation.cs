using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///		Class containing information on an MSEX element library
	/// </summary>
	[PublicAPI]
	public sealed class ElementLibraryInformation : IEquatable<ElementLibraryInformation>,
		IComparable<ElementLibraryInformation>
	{
		public ElementLibraryInformation(MsexLibraryId id, byte dmxRangeMin, byte dmxRangeMax, [NotNull] string name,
			ushort libraryCount, ushort elementCount, uint serialNumber)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Id = id;
			SerialNumber = serialNumber;
			DmxRangeMin = dmxRangeMin;
			DmxRangeMax = dmxRangeMax;
			Name = name;
			LibraryCount = libraryCount;
			ElementCount = elementCount;
		}

		public MsexLibraryId Id { get; }
		public uint SerialNumber { get; }
		public byte DmxRangeMin { get; }
		public byte DmxRangeMax { get; }
		public string Name { get; }
		public ushort LibraryCount { get; }
		public ushort ElementCount { get; }



		public int CompareTo([CanBeNull] ElementLibraryInformation other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			return Id.CompareTo(other.Id);
		}


		public bool Equals([CanBeNull] ElementLibraryInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Id.Equals(other.Id) && SerialNumber == other.SerialNumber && DmxRangeMin == other.DmxRangeMin
			       && DmxRangeMax == other.DmxRangeMax && string.Equals(Name, other.Name) && LibraryCount == other.LibraryCount
			       && ElementCount == other.ElementCount;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is ElementLibraryInformation && Equals((ElementLibraryInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Id.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)SerialNumber;
				hashCode = (hashCode * 397) ^ DmxRangeMin.GetHashCode();
				hashCode = (hashCode * 397) ^ DmxRangeMax.GetHashCode();
				hashCode = (hashCode * 397) ^ Name.GetHashCode();
				hashCode = (hashCode * 397) ^ LibraryCount.GetHashCode();
				hashCode = (hashCode * 397) ^ ElementCount.GetHashCode();
				return hashCode;
			}
		}

		internal static ElementLibraryInformation Deserialize(CitpBinaryReader reader, MsexVersion version)
		{
			var id = reader.ReadLibraryId(version);

			uint serialNumber = 0;
			if (version == MsexVersion.Version1_2)
				serialNumber = reader.ReadUInt32();

			byte dmxRangeMin = reader.ReadByte();
			byte dmxRangeMax = reader.ReadByte();
			string name = reader.ReadString();

			ushort libraryCount = 0;
			ushort elementCount;

			switch (version)
			{
				case MsexVersion.Version1_0:
					elementCount = reader.ReadByte();
					break;
				case MsexVersion.Version1_1:
					libraryCount = reader.ReadByte();
					elementCount = reader.ReadByte();
					break;
				case MsexVersion.Version1_2:
					libraryCount = reader.ReadUInt16();
					elementCount = reader.ReadUInt16();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(version), version, null);
			}

			return new ElementLibraryInformation(id, dmxRangeMin, dmxRangeMax, name, libraryCount, elementCount,
				serialNumber);
		}

		internal void Serialize(CitpBinaryWriter writer, MsexVersion version)
		{
			writer.Write(Id, version);

			if (version == MsexVersion.Version1_2)
				writer.Write(SerialNumber);

			writer.Write(DmxRangeMin);
			writer.Write(DmxRangeMax);
			writer.Write(Name);

			switch (version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)ElementCount);
					break;
				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryCount);
					writer.Write((byte)ElementCount);
					break;
				case MsexVersion.Version1_2:
					writer.Write(LibraryCount);
					writer.Write(ElementCount);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(version), version, null);
			}
		}
	}
}