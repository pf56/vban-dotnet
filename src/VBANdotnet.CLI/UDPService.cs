using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

#nullable enable
namespace VBANdotnet.CLI;


public class UDPService : BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		AudioClient audioClient = new(IPAddress.Any, 6980);
		audioClient.Start();

		while(!stoppingToken.IsCancellationRequested)
		{
			// wait
		}

		audioClient.Stop();
		return Task.CompletedTask;

		// PipeWriter writer = Program.Data.Writer;
		//
		// Console.WriteLine("starting udp client");
		// UdpClient receivingUdpClient = new(45234);
		// IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
		//
		// while(!stoppingToken.IsCancellationRequested)
		// {
		// 	try
		// 	{
		// 		var result = receivingUdpClient.Receive(ref endPoint);
		// 		byte[] receiveBytes = result;
		//
		// 		//VBANHeader header = VBANHeader.Read(receiveBytes);
		//
		// 		Memory<byte> memory = writer.GetMemory(1436);
		// 		int i = 0;
		//
		// 		for(int f = 28; f < receiveBytes.Length; f += 4)
		// 		{
		// 			AudioData audioData = new()
		// 			{
		// 				Left = (short)(receiveBytes[f + 1] << 8 | receiveBytes[f]),
		// 				Right = (short)(receiveBytes[f + 3] << 8 | receiveBytes[f + 2])
		// 			};
		//
		// 			AudioData.Write(audioData).CopyTo(memory[i..(i+4)]);
		//
		// 			i += 4;
		// 		}
		//
		// 		writer.Advance(receiveBytes.Length - 28);
		// 		await writer.FlushAsync(stoppingToken);
		// 	}
		// 	catch(Exception e)
		// 	{
		// 		Console.WriteLine(e.ToString());
		// 	}
		// }
	}
}
