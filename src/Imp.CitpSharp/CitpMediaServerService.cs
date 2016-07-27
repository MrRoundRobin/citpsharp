﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Runs CITP services for a media server device.
	/// </summary>
	[PublicAPI]
	public class CitpMediaServerService : CitpServerService
	{
		private readonly ICitpMediaServerDevice _device;

		public CitpMediaServerService(ICitpLogService logger, ICitpMediaServerDevice device, CitpServiceFlags flags, IPAddress localIp = null)
			: base(logger, device, flags, localIp)
		{
			_device = device;
		}

		public override CitpPeerType DeviceType => CitpPeerType.MediaServer;

		internal override void OnClientInformationPacketReceived(ClientInformationPacket packet, TcpServerConnection client)
		{
			base.OnClientInformationPacketReceived(packet, client);

			var responsePacket = new ServerInformationPacket(packet.Version, _device.Uuid, _device.ProductName,
				(byte)_device.ProductVersionMajor, (byte)_device.ProductVersionMinor, (byte)_device.ProductVersionBugfix,
				_device.SupportedMsexVersions, _device.SupportedLibraryTypes, _device.SupportedThumbnailFormats,
				_device.SupportedStreamFormats, _device.Layers.Select(l => l.DmxSource), packet.RequestResponseIndex);

			client.SendPacket(responsePacket);
		}


		internal override void OnMsexTcpPacketReceived(MsexPacket packet, TcpServerConnection client)
		{
			switch (packet.MessageType)
			{
				case MsexMessageType.GetElementLibraryInformationMessage:
					onGetElementLibraryInformationPacketReceived((GetElementLibraryInformationPacket)packet, client);
					break;

				case MsexMessageType.GetElementInformationMessage:
					onGetElementInformationPacketReceived((GetElementInformationPacket)packet, client);
					break;

				case MsexMessageType.GetElementLibraryThumbnailMessage:
					onGetElementLibraryThumbnailPacketReceived((GetElementLibraryThumbnailPacket)packet, client);
					break;

				case MsexMessageType.GetElementThumbnailMessage:
					onGetElementThumbnailPacketReceived((GetElementThumbnailPacket)packet, client);
					break;

				default:
					base.OnMsexTcpPacketReceived(packet, client);
					break;
			}
		}

		private void onGetElementLibraryInformationPacketReceived(GetElementLibraryInformationPacket packet,
			TcpServerConnection client)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableLibraryInformation))
			{
				SendNack(packet, client);
				return;
			}

			Logger.LogInfo($"{client}: Get element library information packet received");

			var elementInfo = new List<ElementLibraryInformation>();

			var parentLibraryId = packet.LibraryParentId.GetValueOrDefault(MsexLibraryId.Root);

			if (!parentLibraryId.CanHaveChildren)
			{
				Logger.LogWarning($"{client}: Requested element library information for libraries with a parent library ID which can have no child elements");
			}
			else
			{
				var filteredLibraries = filterLibraries(packet.LibraryType, packet.LibraryParentId);

				IEnumerable<byte> libraryNumbers = packet.RequestedLibraryNumbers;

				if (packet.ShouldRequestAllLibraries)
					libraryNumbers = Enumerable.Range(0, 255).Select(i => (byte)i);
				
				var idsToSelect = libraryNumbers.Select(i => parentLibraryId.SetLevel(parentLibraryId.Level + 1).SetLibraryNumber(i));
				
				foreach (var id in idsToSelect)
				{
					ElementLibrary library;

					if (filteredLibraries.TryGetValue(id, out library))
						elementInfo.Add(library.LibraryInformation);
				}
			}

			var responsePacket = new ElementLibraryInformationPacket(packet.Version, packet.LibraryType,
				elementInfo, packet.RequestResponseIndex);

			client.SendPacket(responsePacket);
		}

		private void onGetElementInformationPacketReceived(GetElementInformationPacket packet,
			TcpServerConnection client)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableElementInformation))
			{
				SendNack(packet, client);
				return;
			}

			Logger.LogInfo($"{client}: Get element information packet received");

			ElementLibrary library;
			if (!_device.ElementLibraries.TryGetValue(packet.LibraryId, out library))
			{
				Logger.LogWarning($"{client}: Requested non-existant library - {packet.LibraryId}");
				return;
			}

			var elementNumbers = packet.ShouldRequestAllElements
				? Enumerable.Range(0, 255).Select(i => (byte)i).ToImmutableSortedSet()
				: packet.RequestedElementNumbers;

			var elements = library.Elements.Where(p => elementNumbers.Contains(p.Key)).Select(p => p.Value);

			CitpPacket responsePacket;

			switch (library.LibraryType)
			{
				case MsexLibraryType.Media:
					responsePacket = new MediaElementInformationPacket(packet.Version, packet.LibraryId, 
						elements.Cast<MediaInformation>(), packet.RequestResponseIndex);
					break;

				case MsexLibraryType.Effects:
					responsePacket = new EffectElementInformationPacket(packet.Version, packet.LibraryId,
						elements.Cast<EffectInformation>(), packet.RequestResponseIndex);
					break;

				default:
					if (packet.Version == MsexVersion.Version1_0)
					{
						Logger.LogWarning($"{client}: Requested unsupported library type for MSEX V1.0");
						return;
					}

					responsePacket = new GenericElementInformationPacket(packet.Version, packet.LibraryType, packet.LibraryId,
						elements.Cast<GenericInformation>(), packet.RequestResponseIndex);
					break;
			}

			client.SendPacket(responsePacket);
		}

		private void onGetElementLibraryThumbnailPacketReceived(GetElementLibraryThumbnailPacket packet,
			TcpServerConnection client)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableLibraryThumbnails))
			{
				SendNack(packet, client);
				return;
			}

			Logger.LogInfo($"{client}: Get element library thumbnail packet received");

			var libraryIds = packet.ShouldRequestAllThumbnails 
				? _device.ElementLibraries.Where(p => p.Value.LibraryType == packet.LibraryType).Select(p => p.Key).ToImmutableSortedSet()
				: packet.RequestedLibraryIds;

			var imageRequest = new CitpImageRequest(packet.ThumbnailWidth, packet.ThumbnailHeight, packet.ThumbnailFormat, 
				packet.ThumbnailFlags.HasFlag(MsexThumbnailFlags.PreserveAspectRatio), packet.Version == MsexVersion.Version1_0);

			foreach (var id in libraryIds)
			{
				ElementLibrary library;
				if (!_device.ElementLibraries.TryGetValue(id, out library))
				{
					Logger.LogWarning($"{client}: Requested thumbnail for non-existant library {id}");
					continue;
				}

				if (library.LibraryType != packet.LibraryType)
				{
					Logger.LogWarning($"{client}: Requested thumbnail for library {id} with expected type {packet.LibraryType}, actual type is {library.LibraryType}");
					continue;
				}

				var image = _device.GetElementLibraryThumbnail(imageRequest, library.LibraryInformation);
				if (image == null)
				{
					Logger.LogWarning($"{client}: Failed to get thumbnail for library {id}");
					continue;
				}

				var responsePacket = new ElementLibraryThumbnailPacket(packet.Version, packet.LibraryType, id, 
					image.Request.Format, (ushort)image.ActualWidth, (ushort)image.ActualHeight,
					image.Data, packet.RequestResponseIndex);

				client.SendPacket(responsePacket);
			}

			
		}

		private void onGetElementThumbnailPacketReceived(GetElementThumbnailPacket packet,
			TcpServerConnection client)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableElementThumbnails))
			{
				SendNack(packet, client);
				return;
			}

			Logger.LogInfo($"{client}: Get element thumbnail packet received");

			ElementLibrary library;
			if (!_device.ElementLibraries.TryGetValue(packet.LibraryId, out library))
			{
				Logger.LogWarning($"{client}: Requested thumbnails for elements in non-existant library {packet.LibraryId}");
				return;
			}

			if (library.LibraryType != packet.LibraryType)
			{
				Logger.LogWarning($"{client}: Requested thumbnails for elements in library {packet.LibraryId} with expected type {packet.LibraryType}, "
				                  + $"actual type is {library.LibraryType}");
				return;
			}

			var elementNumbers = packet.ShouldRequestAllThumbnails
				? library.Elements.Keys.ToImmutableSortedSet()
				: packet.RequestedElementNumbers;

			var imageRequest = new CitpImageRequest(packet.ThumbnailWidth, packet.ThumbnailHeight, packet.ThumbnailFormat,
				packet.ThumbnailFlags.HasFlag(MsexThumbnailFlags.PreserveAspectRatio), packet.Version == MsexVersion.Version1_0);

			foreach (byte i in elementNumbers)
			{
				ElementInformation info;

				if (!library.Elements.TryGetValue(i, out info))
				{
					Logger.LogWarning($"{client}: Requested thumbnail for non-existant element {i} in library {packet.LibraryId}");
					continue;
				}

				var image = _device.GetElementThumbnail(imageRequest, library.LibraryInformation, info);
				if (image == null)
				{
					Logger.LogWarning($"{client}: Failed to get thumbnail for element {i} in library {packet.LibraryId}");
					continue;
				}

				var responsePacket = new ElementThumbnailPacket(packet.Version, packet.LibraryType, packet.LibraryId, i,
					image.Request.Format, (ushort)image.ActualWidth, (ushort)image.ActualHeight,
					image.Data, packet.RequestResponseIndex);

				client.SendPacket(responsePacket);
			}

		}



		private IReadOnlyDictionary<MsexLibraryId, ElementLibrary> filterLibraries(MsexLibraryType libraryType,
			MsexLibraryId? libraryParentId)
		{
			return _device.ElementLibraries.Where(
				p => p.Value.LibraryType == libraryType && p.Key.IsChildOf(libraryParentId ?? MsexLibraryId.Root))
				.ToDictionary(p => p.Key, p => p.Value);
		}
	}
}