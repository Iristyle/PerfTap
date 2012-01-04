namespace PerfTap.Net
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	public static class PacketBuilder
	{
		private static byte[] _terminator = Encoding.UTF8.GetBytes("\n");

		public static IEnumerable<byte[]> ToMaximumBytePackets(this IEnumerable<string> metrics)
		{
			return ToMaximumBytePackets(metrics, 512);
		}

		public static IEnumerable<byte[]> ToMaximumBytePackets(this IEnumerable<string> metrics, int packetSize)
		{
			List<byte> packet = new List<byte>(packetSize);

			foreach (string metric in metrics)
			{
				var bytes = Encoding.UTF8.GetBytes(metric);
				if (packet.Count + _terminator.Length + bytes.Length <= packetSize)
				{
					packet.AddRange(bytes);
					packet.AddRange(_terminator);
				}
				else if (bytes.Length >= packetSize)
				{
					yield return bytes;
				}
				else
				{
					yield return packet.ToArray();
					packet.Clear();
					packet.AddRange(bytes);
					packet.AddRange(_terminator);
				}
			}

			if (packet.Count > 0)
			{
				yield return packet.ToArray();
			}
		}
	}
}