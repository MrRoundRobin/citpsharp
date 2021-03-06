using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///		Immutable class representing information on a single video source available for streaming via CITP
	/// </summary>
	[PublicAPI]
	public sealed class VideoSourceInformation : IEquatable<VideoSourceInformation>,
		IComparable<VideoSourceInformation>
	{
		internal static VideoSourceInformation Deserialize(CitpBinaryReader reader)
		{
			ushort sourceIdentifier = reader.ReadUInt16();
			string sourceName = reader.ReadString();
			byte physicalOutput = reader.ReadByte();
			byte layerNumber = reader.ReadByte();
			var flags = (MsexVideoSourcesFlags)reader.ReadUInt16();
			ushort width = reader.ReadUInt16();
			ushort height = reader.ReadUInt16();

			return new VideoSourceInformation(sourceIdentifier, sourceName, flags, width, height, physicalOutput,
				layerNumber);
		}

		/// <summary>
		///		Constructs video source with all required information
		/// </summary>
		/// <param name="sourceIdentifier"></param>
		/// <param name="sourceName"></param>
		/// <param name="flags"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="physicalOutput"></param>
		/// <param name="layerNumber"></param>
		public VideoSourceInformation(ushort sourceIdentifier, string sourceName, MsexVideoSourcesFlags flags,
			ushort width, ushort height, byte? physicalOutput = null, byte? layerNumber = null)
		{
			SourceIdentifier = sourceIdentifier;
			SourceName = sourceName;
			PhysicalOutput = physicalOutput;
			LayerNumber = layerNumber;
			Flags = flags;
			Width = width;
			Height = height;
		}

		/// <summary>
		///		Unique identifier for this video source
		/// </summary>
		public ushort SourceIdentifier { get; }

		/// <summary>
		///		User-facing name of this video source
		/// </summary>
		public string SourceName { get; }

		/// <summary>
		///		Optional physical output index.
		/// </summary>
		public byte? PhysicalOutput { get; }

		/// <summary>
		///		Optional layer index
		/// </summary>
		public byte? LayerNumber { get; }

		/// <summary>
		///		Additional information on this source
		/// </summary>
		public MsexVideoSourcesFlags Flags { get; }

		/// <summary>
		///		Width of video source
		/// </summary>
		public ushort Width { get; }

		/// <summary>
		///		Height of video source
		/// </summary>
		public ushort Height { get; }

		/// <summary>
		///		CompareTo implementation orders by <see cref="SourceIdentifier"/>
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo([CanBeNull] VideoSourceInformation other)
		{
			return ReferenceEquals(other, null) ? 1 : SourceIdentifier.CompareTo(other.SourceIdentifier);
		}

		internal void Serialize(CitpBinaryWriter writer)
		{
			writer.Write(SourceIdentifier);
			writer.Write(SourceName);

			if (PhysicalOutput.HasValue)
				writer.Write(PhysicalOutput.Value);
			else
				writer.Write((byte)0xFF);

			if (LayerNumber.HasValue)
				writer.Write(LayerNumber.Value);
			else
				writer.Write((byte)0xFF);

			writer.Write((ushort)Flags);

			writer.Write(Width);
			writer.Write(Height);
		}

		/// <summary>
		///		Compares all properties of this video source with another
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals([CanBeNull] VideoSourceInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return SourceIdentifier == other.SourceIdentifier && string.Equals(SourceName, other.SourceName)
			       && PhysicalOutput == other.PhysicalOutput && LayerNumber == other.LayerNumber && Flags == other.Flags
			       && Width == other.Width && Height == other.Height;
		}

		/// <summary>
		///		Compares all properties of this video source with another
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is VideoSourceInformation && Equals((VideoSourceInformation)obj);
		}

		/// <summary>
		///		Returns hashcode based on all properties of this video source
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = SourceIdentifier.GetHashCode();
				hashCode = (hashCode * 397) ^ SourceName.GetHashCode();
				hashCode = (hashCode * 397) ^ PhysicalOutput.GetHashCode();
				hashCode = (hashCode * 397) ^ LayerNumber.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)Flags;
				hashCode = (hashCode * 397) ^ Width.GetHashCode();
				hashCode = (hashCode * 397) ^ Height.GetHashCode();
				return hashCode;
			}
		}
	}
}