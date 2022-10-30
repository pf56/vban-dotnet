using System.IO.Pipelines;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

#nullable enable
namespace VBANdotnet.CLI;

public class UDPService : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		PipeWriter writer = Program.Data.Writer;

		Console.WriteLine("starting udp client");
		UdpClient receivingUdpClient = new(45234);

		while(!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var result = await receivingUdpClient.ReceiveAsync(stoppingToken);
				byte[] receiveBytes = result.Buffer;

				//VBANHeader header = VBANHeader.Read(receiveBytes);

				Memory<byte> memory = writer.GetMemory(1436);
				int i = 0;

				for(int f = 28; f < receiveBytes.Length; f += 4)
				{
					AudioData audioData = new()
					{
						Left = (short)(receiveBytes[f + 1] << 8 | receiveBytes[f]),
						Right = (short)(receiveBytes[f + 3] << 8 | receiveBytes[f + 2])
					};

					AudioData.Write(audioData).CopyTo(memory[i..(i+4)]);

					writer.Advance(4);
					await writer.FlushAsync(stoppingToken);

					i += 4;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}
	}
}