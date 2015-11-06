﻿//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	internal class CitpId : Attribute
	{
		public CitpId(string id)
		{
			Id = Encoding.UTF8.GetBytes(id.Substring(0, 4));
		}

		public byte[] Id { get; private set; }

		public string IdString
		{
			get { return Encoding.UTF8.GetString(Id); }
		}
	}



	internal static class CitpEnumHelper
	{
		private static readonly Dictionary<Type, Dictionary<string, Enum>> CitpIdMaps =
			new Dictionary<Type, Dictionary<string, Enum>>();

		/// <summary>
		///     Gets an attribute on an enum field value
		/// </summary>
		/// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
		/// <param name="enumVal">The enum value</param>
		/// <returns>The attribute of type T that exists on the enum value</returns>
		[CanBeNull]
		public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
		{
			var type = enumVal.GetType();
			var memInfo = type.GetMember(enumVal.ToString());
			var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
			return attributes.Length > 0 ? (T)attributes[0] : null;
		}

		public static T GetEnumFromIdString<T>(string s) where T : struct, IConvertible
		{
			Dictionary<string, Enum> map;

			if (CitpIdMaps.TryGetValue(typeof(T), out map))
				return (T)(object)map[s];

			var values = Enum.GetValues(typeof(T)).Cast<Enum>();

			map = values.ToDictionary(v => v.GetAttributeOfType<CitpId>().IdString);

			CitpIdMaps.Add(typeof(T), map);

			return (T)(object)map[s];
		}
	}



	internal enum CitpLayerType : uint
	{
		[CitpId("PINF")] PeerInformationLayer,
		[CitpId("SDMX")] SendDmxLayer,
		[CitpId("FPTC")] FixturePatchLayer,
		[CitpId("FSEL")] FixtureSelectionLayer,
		[CitpId("FINF")] FixtureInformationLayer,
		[CitpId("MSEX")] MediaServerExtensionsLayer
	}



	internal enum PinfMessageType : uint
	{
		[CitpId("PNam")] PeerNameMessage,
		[CitpId("PLoc")] PeerLocationMessage
	}



	internal enum SdmxMessageType : uint
	{
		[CitpId("Capa")] CapabilitiesMessage,
		[CitpId("UNam")] UniverseNameMessage,
		[CitpId("EnId")] EncryptionIdentifierMessage,
		[CitpId("ChBk")] ChannelBlockMessage,
		[CitpId("ChLs")] ChannelListMessage,
		[CitpId("SXSr")] SetExternalSourceMessage,
		[CitpId("SXUS")] SetExternalUniverseSourceMessage
	}



	internal enum FptcMessageType : uint
	{
		[CitpId("Ptch")] PatchMessage,
		[CitpId("UPtc")] UnpatchMessage,
		[CitpId("SPtc")] SendPatchMessage
	}



	internal enum FselMessageType : uint
	{
		[CitpId("Sele")] SelectMessage,
		[CitpId("DeSe")] DeselectMessage
	}



	internal enum FinfMessageType : uint
	{
		[CitpId("SFra")] SendFramesMessage,
		[CitpId("Fram")] FramesMessage,
		[CitpId("SPos")] SendPositionMessage,
		[CitpId("Posi")] PositionMessage,
		[CitpId("LSta")] LiveStatusMessage
	}



	internal enum MsexMessageType : uint
	{
		[CitpId("CInf")] ClientInformationMessage,
		[CitpId("SInf")] ServerInformationMessage,
		[CitpId("Nack")] NegativeAcknowledgeMessage,
		[CitpId("LSta")] LayerStatusMessage,
		[CitpId("GELI")] GetElementLibraryInformationMessage,
		[CitpId("ELIn")] ElementLibraryInformationMessage,
		[CitpId("ELUp")] ElementLibraryUpdatedMessage,
		[CitpId("GEIn")] GetElementInformationMessage,
		[CitpId("MEIn")] MediaElementInformationMessage,
		[CitpId("EEIn")] EffectElementInformationMessage,
		[CitpId("GLEI")] GenericElementInformationMessage,
		[CitpId("GELT")] GetElementLibraryThumbnailMessage,
		[CitpId("ELTh")] ElementLibraryThumbnailMessage,
		[CitpId("GETh")] GetElementThumbnailMessage,
		[CitpId("EThn")] ElementThumbnailMessage,
		[CitpId("GVSr")] GetVideoSourcesMessage,
		[CitpId("VSrc")] VideoSourcesMessage,
		[CitpId("RqSt")] RequestStreamMessage,
		[CitpId("StFr")] StreamFrameMessage
	}



	internal enum CitpPeerType
	{
		LightingConsole,
		MediaServer,
		Visualizer,
		OperationHub,
		Unknown
	}



	internal enum SdmxCapability : ushort
	{
		ChannelList = 1,
		ExternalSource = 2,
		PerUniverseExternalSources = 3,
		ArtNetExternalSources = 101,
		Bsre131ExternalSources = 102,
		EtcNet2ExternalSources = 103,
		MaNetExternalSources = 104
	}



	internal class CitpVersion : Attribute, IEquatable<CitpVersion>
	{
		public CitpVersion(byte majorVersion, byte minorVersion)
		{
			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
		}

		public byte MajorVersion { get; private set; }
		public byte MinorVersion { get; private set; }

		public bool Equals([CanBeNull] CitpVersion other)
		{
			if (other == null)
				return false;

			return MajorVersion == other.MajorVersion && MinorVersion == other.MinorVersion;
		}

		public byte[] ToByteArray()
		{
			return new[] {MajorVersion, MinorVersion};
		}

		public override bool Equals([CanBeNull] object obj)
		{
			var m = obj as CitpElementLibraryInformation;
			if (m == null)
				return false;

			return Equals(m);
		}

		public override int GetHashCode()
		{
			return MajorVersion.GetHashCode()
			       ^ MinorVersion.GetHashCode();
		}
	}



	public enum MsexVersion
	{
		[CitpVersion(0, 0)] UnsupportedVersion,
		[CitpVersion(1, 0)] Version10,
		[CitpVersion(1, 1)] Version11,
		[CitpVersion(1, 2)] Version12
	}



	public enum MsexLibraryType : byte
	{
		Media = 1,
		Effects = 2,
		Cues = 3,
		Crossfades = 4,
		Mask = 5,
		BlendPresets = 6,
		EffectPresets = 7,
		ImagePresets = 8,
		Meshes = 9
	}



	public enum MsexImageFormat : uint
	{
		[CitpId("RGB8")] Rgb8,
		[CitpId("PNG ")] Png,
		[CitpId("JPEG")] Jpeg,
		[CitpId("fJPG")] FragmentedJpeg,
		[CitpId("fPNG")] FragmentedPng
	}



	[Flags]
	public enum MsexLayerStatusFlags : uint
	{
		None = 0x0000,
		MediaPlaying = 0x0001,
		MediaPlaybackReverse = 0x0002,
		MediaPlaybackLooping = 0x0004,
		MediaPlaybackBouncing = 0x0008,
		MediaPlaybackRandom = 0x0010,
		MediaPaused = 0x0020
	}



	[Flags]
	public enum MsexElementLibraryUpdatedFlags : byte
	{
		None = 0x00,
		ExistingElementsUpdated = 0x01,
		ElementsAddedOrRemoved = 0x02,
		SubLibrariesUpdated = 0x04,
		SubLibrariesAddedOrRemoved = 0x08
	}



	[Flags]
	public enum MsexThumbnailFlags : byte
	{
		None = 0x00,
		PreserveAspectRatio = 0x01
	}



	[Flags]
	public enum MsexVideoSourcesFlags : ushort
	{
		None = 0x0000,
		WithoutEffects = 0x0001
	}
}