using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
//using TerraFX.Interop.PulseAudio;
using VBANdotnet.CLI;
using Xt;


public static class Program
{
	private static readonly XtMix Mix = new(48000, XtSample.Int16);
	private static readonly XtChannels Channels = new(0, 0, 2, 0);
	private static readonly XtFormat Format = new(Mix, Channels);

	public static AudioData[] Buffer = new AudioData[1000 * 4];
	public static int readerPosition = 0;
	public static int writerPosition = 0;

	[STAThread]
	public static unsafe void Main()
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
			for(int i = 0; i < size; i++)
			{
				readerPosition += 1;
				readerPosition %= Buffer.Length;

				if(readerPosition > 0 && readerPosition == writerPosition)
				{
					readerPosition -= 1;
				}

				AudioData audioData = Buffer[readerPosition];
				output[i * 2] = audioData.Left;
				output[i * 2 + 1] = audioData.Right;

				//Console.WriteLine("Read: " + readerPosition + " " + audioData.Left + " " + audioData.Right);
			}

			safe.Unlock(in buffer);
			return 0;
		}

		Console.WriteLine("starting");

		XtBufferSize size = device.GetBufferSize(Format);
		XtStreamParams streamParams = new(true, ProcessUdp, null, null);
		XtDeviceStreamParams deviceParams = new(in streamParams, in Format, size.min);
		using XtStream stream = device.OpenStream(in deviceParams, null);
		using XtSafeBuffer safe = XtSafeBuffer.Register(stream);

		// pulse
/*		string name = "Fooapp";
		string streamName = "fooStream";

		byte[] bName = Encoding.ASCII.GetBytes(name);
		byte[] bStreamName = Encoding.ASCII.GetBytes(streamName);

		sbyte[] sbName = Array.ConvertAll(bName, Convert.ToSByte);
		sbyte[] sbStreamName = Array.ConvertAll(bStreamName, Convert.ToSByte);

		fixed(sbyte* psbName = sbName)
		fixed(sbyte* psbStreamName = sbStreamName)
		{
			pa_threaded_mainloop* loop = PulseAudio.pa_threaded_mainloop_new();
			pa_mainloop_api* loop_api = PulseAudio.pa_threaded_mainloop_get_api(loop);

			pa_context* context = PulseAudio.pa_context_new(loop_api, psbName);
			PulseAudio.pa_context_set_state_callback(context, &context_state_cb, loop);
			PulseAudio.pa_threaded_mainloop_lock(loop);

			PulseAudio.pa_threaded_mainloop_start(loop);
			PulseAudio.pa_context_connect(context, null, pa_context_flags_t.PA_CONTEXT_NOAUTOSPAWN, null);

			while(true) {
				pa_context_state_t context_state = PulseAudio.pa_context_get_state(context);
				Debug.Assert(PulseAudio.PA_CONTEXT_IS_GOOD(context_state) == 1);
				if (context_state == pa_context_state_t.PA_CONTEXT_READY) break;
				PulseAudio.pa_threaded_mainloop_wait(loop);
			}

			Console.WriteLine("initialized");

			// Create a playback stream
			pa_sample_spec sample_specifications;
			sample_specifications.format = PulseAudio.PA_SAMPLE_S32NE;
			sample_specifications.rate = 48000;
			sample_specifications.channels = 2;

			pa_channel_map map;
			PulseAudio.pa_channel_map_init_stereo(&map);

			pa_stream* stream = PulseAudio.pa_stream_new(context, psbStreamName, &sample_specifications, &map);
			PulseAudio.pa_stream_set_state_callback(stream, &stream_state_cb, loop);
			PulseAudio.pa_stream_set_write_callback(stream, &stream_write_cb, loop);

			// recommended settings, i.e. server uses sensible values
			pa_buffer_attr buffer_attr;
			buffer_attr.maxlength = (uint.MaxValue) -1;
			buffer_attr.tlength = (uint.MaxValue) -1;
			buffer_attr.prebuf = (uint.MaxValue) -1;
			buffer_attr.minreq = (uint.MaxValue) -1;

			// Settings copied as per the chromium browser source
			pa_stream_flags_t stream_flags = pa_stream_flags_t.PA_STREAM_START_CORKED |
														pa_stream_flags_t.PA_STREAM_INTERPOLATE_TIMING |
														pa_stream_flags_t.PA_STREAM_NOT_MONOTONIC |
														pa_stream_flags_t.PA_STREAM_AUTO_TIMING_UPDATE |
														pa_stream_flags_t.PA_STREAM_ADJUST_LATENCY;

			Console.WriteLine("connecting playback");
			// Connect stream to the default audio output sink
			Debug.Assert(PulseAudio.pa_stream_connect_playback(stream, null, &buffer_attr, stream_flags, null,
				null) == 0);

			// Wait for the stream to be ready
			while(true)
			{
				Console.WriteLine("getting stream state");
				pa_stream_state_t stream_state = PulseAudio.pa_stream_get_state(stream);
				Debug.Assert(PulseAudio.PA_STREAM_IS_GOOD(stream_state) == 1);
				Console.WriteLine("stream state: " + stream_state);
				if (stream_state == pa_stream_state_t.PA_STREAM_READY) break;
				PulseAudio.pa_threaded_mainloop_wait(loop);
			}

			Console.WriteLine("stream ready");

			PulseAudio.pa_threaded_mainloop_unlock(loop);

			// Uncork the stream so it will start playing
			PulseAudio.pa_stream_cork(stream, 0, &stream_success_cb, loop);
		}
*/
		// PulseAudio.pa_simple_new()
		// PulseAudio.pa_context_get_sink_info_list()

		AudioClient audioClient = new(IPAddress.Any, 6980);
		audioClient.Start();
		stream.Start();

/*		ScreamClient screamClient = new("0.0.0.0", 4010);
		screamClient.SetupMulticast(true);
		screamClient.Multicast = "239.255.77.77";
		screamClient.Connect();
*/

		while(true)
		{
			Thread.Sleep(100);
		}
	}

/*	[UnmanagedCallersOnly]
	private static unsafe void context_state_cb(pa_context *context, void *loop) {
		Console.WriteLine("context_state_cb");
		PulseAudio.pa_threaded_mainloop_signal((pa_threaded_mainloop*)loop, 0);
	}

	[UnmanagedCallersOnly]
	private static unsafe void stream_state_cb(pa_stream *s, void *loop) {
		Console.WriteLine("stream_state_cb");
		PulseAudio.pa_threaded_mainloop_signal((pa_threaded_mainloop*)loop, 0);
	}

	[UnmanagedCallersOnly]
	static unsafe void stream_write_cb(pa_stream *stream, nuint requested_bytes, void *userdata) {
		//Console.WriteLine("writing stream " + requested_bytes);
		nuint bytes_remaining = requested_bytes;
		while (bytes_remaining > 0) {
			byte *buffer = null;
			nuint bytes_to_fill = 48000;
			nuint i;

			if (bytes_to_fill > bytes_remaining) bytes_to_fill = bytes_remaining;

			PulseAudio.pa_stream_begin_write(stream, (void**) &buffer, &bytes_to_fill);

			for (i = 0; i < bytes_to_fill; i += 2) {
				buffer[i] = (byte)((i%100) * 40 / 100 + 44);
				buffer[i+1] = (byte)((i%100) * 40 / 100 + 44);
			}

			PulseAudio.pa_stream_write(stream, buffer, bytes_to_fill, null, 0, pa_seek_mode_t.PA_SEEK_RELATIVE);

			bytes_remaining -= bytes_to_fill;
		}
	}

	[UnmanagedCallersOnly]
	static unsafe void stream_success_cb(pa_stream *stream, int success, void *userdata) {
		return;
	}
*/
}
