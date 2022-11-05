using System.Net;
using Microsoft.Extensions.Hosting;
using VBANdotnet.CLI;
using Xt;


public static class Program
{
	private static readonly XtMix Mix = new(48000, XtSample.Int16);
	private static readonly XtChannels Channels = new(0, 0, 2, 0);
	private static readonly XtFormat Format = new(Mix, Channels);

	public static AudioData[] Buffer = new AudioData[3840];
	public static int readerPosition = 0;
	public static int writerPosition = 0;

	[STAThread]
	public static void Main()
	{
		// setup XtAudio
		using XtPlatform platform = XtAudio.Init(null, IntPtr.Zero);
		XtSystem system = platform.SetupToSystem(XtSetup.ConsumerAudio);
		XtService service = platform.GetService(system);
		if(service == null) return;

		// get default output device
		string defaultOutput = service.GetDefaultDeviceId(true);
		if(defaultOutput == null) return;
		using XtDevice device = service.OpenDevice(defaultOutput);
		if(!device.SupportsFormat(Format)) return;

		int ProcessUdp(XtStream stream, in XtBuffer buffer, object user)
		{
			XtSafeBuffer safe = XtSafeBuffer.Get(stream);
			safe.Lock(in buffer);
			short[] output = (short[])safe.GetOutput();


			int size = buffer.frames;
			//AudioData[] audioDatas = Buffer[readerPosition..(readerPosition + size)];

			for(int i = 0; i < size; i++)
			{
				readerPosition += 1;

				// if(readerPosition == writerPosition)
				// 	readerPosition--;

				readerPosition %= Buffer.Length;

				// if(readerPosition == 0)
				// {
				// 	writerPosition = 0;
				// }

				AudioData audioData = Buffer[readerPosition];
				output[i * 2] = audioData.Left;
				output[i * 2 + 1] = audioData.Right;

				//Console.WriteLine("Read: " + readerPosition + " " + audioData.Left + " " + audioData.Right);
			}

			safe.Unlock(in buffer);
			return 0;
		}

		XtBufferSize size = device.GetBufferSize(Format);
		XtStreamParams streamParams = new(true, ProcessUdp, null, null);
		XtDeviceStreamParams deviceParams = new(in streamParams, in Format, size.min);
		using XtStream stream = device.OpenStream(in deviceParams, null);
		using XtSafeBuffer safe = XtSafeBuffer.Register(stream);
		stream.Start();

		AudioClient audioClient = new(IPAddress.Any, 45234);
		audioClient.Start();

		while(true)
		{

		}

		// using IHost host = Host.CreateDefaultBuilder()
		// 	.ConfigureServices(services =>
		// 	{
		// 		//services.AddHostedService<UDPService>();
		// 	})
		// 	.Build();
		//
		// await host.RunAsync();
	}
}