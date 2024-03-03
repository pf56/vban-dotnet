using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace VBANdotnet.CLI;

public class AudioClient(IPAddress address, int port) : UdpServer(address, port)
{
	protected override void OnStarted()
	{
		// Start receiving datagrams
		ReceiveAsync();
	}

	protected override void OnReceived(
		EndPoint endpoint,
		byte[] receiveBytes,
		long offset,
		long size
		)
	{
		try
		{
			//VBANHeader header = VBANHeader.Read(receiveBytes);

			for(int f = 28; f < size; f += 4)
			{
				AudioData audioData = new()
				{
					Left = (short)((receiveBytes[f + 1] << 8) | receiveBytes[f]),
					Right = (short)((receiveBytes[f + 3] << 8) | receiveBytes[f + 2])
				};

				Program.Data.Writer.TryWrite(audioData);
			}
		}
		catch(Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		// Continue receive datagrams
		ReceiveAsync();
	}

	protected override void OnError(SocketError error)
	{
		Console.WriteLine($"{nameof(AudioClient)} caught an error with code {error}");
	}
}