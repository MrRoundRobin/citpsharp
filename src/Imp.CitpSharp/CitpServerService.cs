﻿using System;
using System.Net;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Base class for CITP services implementing a TCP server and streaming video services
	/// </summary>
	[PublicAPI]
	public abstract class CitpServerService : CitpService
	{
		private static readonly TimeSpan StreamTimerInterval = TimeSpan.FromMilliseconds(1000d / 60d);

		private readonly ICitpServerDevice _device;
		private readonly RegularTimer _streamTimer;

		private readonly TcpServer _tcpServer;
		private readonly StreamManager _streamManager;

		private bool _isDisposed;

		/// <summary>
		///		Constructs base <see cref="CitpServerService"/>
		/// </summary>
		/// <param name="logger">Implementation of <see cref="ICitpLogService"/></param>
		/// <param name="device">Implementation of <see cref="ICitpServerDevice"/> used to resolve requests from service</param>
		/// <param name="flags">Optional flags used to configure service behavior</param>
		/// <param name="preferredTcpListenPort">Service will attempt to start on this port if available, otherwise an available port will be used</param>
		/// <param name="localIp">Address of network interface to start network services on</param>
		protected CitpServerService(ICitpLogService logger, ICitpServerDevice device, CitpServiceFlags flags = CitpServiceFlags.None, 
			int preferredTcpListenPort = 0, IPAddress localIp = null)
			: base(logger, device, flags, localIp)
		{
			_device = device;

			_streamManager = new StreamManager(logger, device);

			if (preferredTcpListenPort < ushort.MinValue || preferredTcpListenPort > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(preferredTcpListenPort), $"Out of valid range {ushort.MinValue} - {ushort.MaxValue}");

			if (!TcpServer.IsTcpPortAvailable(preferredTcpListenPort))
			{
				logger.LogWarning($"Preferred TCP listen port ({preferredTcpListenPort}) is unavailable, picking new port");
				preferredTcpListenPort = 0;
			}

			_tcpServer = new TcpServer(logger, new IPEndPoint(localIp ?? IPAddress.Any, preferredTcpListenPort));
			_tcpServer.ConnectionOpened += OnTcpConnectionOpened;
			_tcpServer.ConnectionClosed += OnTcpConnectionClosed;
			_tcpServer.PacketReceived += OnTcpPacketReceived;

			_streamTimer = new RegularTimer(StreamTimerInterval);
			_streamTimer.Elapsed += (s, e) => processStreamFrameRequests();

			if (!flags.HasFlag(CitpServiceFlags.DisableStreaming) && flags.HasFlag(CitpServiceFlags.RunStreamThread))
				_streamTimer.Start();
		}

		/// <summary>
		///		Port used by clients to connect via TCP
		/// </summary>
		public int TcpListenPort => _tcpServer.ListenPort;

		internal TcpServer TcpServer => _tcpServer;

		/// <summary>
		///		Gets stream frame requests from device for streams which require output
		/// </summary>
		/// <param name="sourceId">Source ID to process stream frame requests for, or null to process all stream IDs</param>
		public void ProcessStreamFrameRequests(int? sourceId = null)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableStreaming))
				throw new InvalidOperationException("Streaming disabled by configuration flags");

			if (Flags.HasFlag(CitpServiceFlags.RunStreamThread))
				throw new InvalidOperationException("Configuration flags set to run stream requests through internal service thread");

			processStreamFrameRequests(sourceId);
		}

		/// <summary>
		///		Dispose pattern implementation
		/// </summary>
		/// <param name="isDisposing"></param>
		protected override void Dispose(bool isDisposing)
		{
			if (!_isDisposed)
			{
				if (isDisposing)
				{
					_streamTimer.Dispose();
					_tcpServer.Dispose();
				}
			}

			base.Dispose(isDisposing);
			_isDisposed = true;
		}

		/// <summary>
		///		Sends peer location packet via UDP
		/// </summary>
		protected override void SendPeerLocationPacket()
		{
			UdpService.SendPacket(new PeerLocationPacket(true, (ushort)TcpListenPort, DeviceType, _device.PeerName, _device.State));
		}


		internal virtual void OnTcpConnectionOpened(object sender, TcpServerConnection client)
		{
			client.SendPacket(new PeerNamePacket(_device.PeerName));
		}

		internal virtual void OnTcpConnectionClosed(object sender, TcpServerConnection client) { }

		internal virtual void OnTcpPacketReceived(object sender, TcpPacketReceivedEventArgs e)
		{
			switch (e.Packet.LayerType)
			{
				case CitpLayerType.MediaServerExtensionsLayer:
					OnMsexTcpPacketReceived((MsexPacket)e.Packet, e.Client);
					break;

				case CitpLayerType.PeerInformationLayer:
					OnPinfTcpPacketReceived((PinfPacket)e.Packet, e.Client);
					break;
			}
		}

		internal virtual void OnPinfTcpPacketReceived(PinfPacket packet, TcpServerConnection client)
		{
			switch (packet.MessageType)
			{
				case PinfMessageType.PeerNameMessage:
				{
					var peerNamePacket = (PeerNamePacket)packet;

					var peer = PeerRegistry.FindPeer(peerNamePacket.Name, client.Ip)
					           ?? PeerRegistry.AddPeer(peerNamePacket, client.Ip);

					client.Peer = peer;
				}
					break;
			}
		}

		internal virtual void OnMsexTcpPacketReceived(MsexPacket packet, TcpServerConnection client)
		{
			switch (packet.MessageType)
			{
				case MsexMessageType.ClientInformationMessage:
					OnClientInformationPacketReceived((ClientInformationPacket)packet, client);
					break;

				case MsexMessageType.GetVideoSourcesMessage:
					OnGetVideoSourcesPacketReceived((GetVideoSourcesPacket)packet, client);
					break;

				case MsexMessageType.RequestStreamMessage:
					OnRequestStreamPacketReceived((RequestStreamPacket)packet, client);
					break;

				default:
					SendNack(packet, client);
					break;
			}
		}

		internal virtual void OnClientInformationPacketReceived(ClientInformationPacket packet, TcpServerConnection client)
		{
			Logger.LogInfo($"{client}: Client information packet received");

			client.SupportedMsexVersions = packet.SupportedMsexVersions;
		}

		internal virtual void OnGetVideoSourcesPacketReceived(GetVideoSourcesPacket packet, TcpServerConnection client)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableStreaming))
			{
				SendNack(packet, client);
				return;
			}

			Logger.LogInfo($"{client}: Get video sources packet received");

			var response = new VideoSourcesPacket(packet.Version, _device.VideoSourceInformation.Values,
				packet.RequestResponseIndex);

			client.SendPacket(response);
		}

		internal virtual void OnRequestStreamPacketReceived(RequestStreamPacket packet, TcpServerConnection client)
		{
			if (Flags.HasFlag(CitpServiceFlags.DisableStreaming))
			{
				SendNack(packet, client);
				return;
			}

			Logger.LogInfo($"{client}: Requested stream ID {packet.SourceId} @ {packet.FrameWidth}x{packet.FrameHeight}, {packet.Fps} fps,"
			               + $" {packet.FrameFormat.GetCustomAttribute<CitpId>().IdString}, timeout {packet.Timeout} sec");

			if (client.Peer == null)
			{
				Logger.LogWarning("Cannot add request for stream, CITP peer info not known for TCP client");
				return;
			}

			_streamManager.AddRequest(client.Peer, packet);
		}

		internal void SendNack(MsexPacket packet, TcpServerConnection client)
		{
			Logger.LogInfo($"{client}: Nack sent for message type {packet.MessageType}");
			client.SendPacket(new NegativeAcknowledgePacket(packet.Version, packet.MessageType, packet.RequestResponseIndex));
		}


		private void processStreamFrameRequests(int? sourceId = null)
		{
			var packets = _streamManager.GetPackets((ushort?)sourceId);

			foreach (var p in packets)
				UdpService.SendPacket(p);
		}
	}
}