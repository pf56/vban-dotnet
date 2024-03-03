namespace VBANdotnet.CLI;

public struct AudioData
{
	public short Left { get; set; }
	public short Right { get; set; }

	public static byte[] Write(AudioData data)
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream);

		writer.Write(data.Left);
		writer.Write(data.Right);

		return stream.ToArray();
	}

	public static AudioData Read(byte[] bytes)
	{
		using BinaryReader reader = new(new MemoryStream(bytes));
		AudioData data = default;

		data.Left = reader.ReadInt16();
		data.Right = reader.ReadInt16();

		return data;
	}
}