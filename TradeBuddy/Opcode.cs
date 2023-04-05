using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TradeRecorder.Model;

namespace TradeRecorder
{
	public class Opcode
	{
		public static async Task<List<Opcodes>> GetOpcodesFromGitHub() {
			return await GetOpcodesFromGitHub(CancellationToken.None);
		}
		public static async Task<List<Opcodes>> GetOpcodesFromGitHub(CancellationToken cancellationToken ) {
			var uriBuilder = new UriBuilder($"https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json");

			cancellationToken.ThrowIfCancellationRequested();

			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
			using var result = await client.GetAsync(uriBuilder.Uri, cancellationToken);

			if (result.StatusCode != HttpStatusCode.OK) {
				throw new HttpRequestException("Invalid status code " + result.StatusCode, null, result.StatusCode);
			}
			await using var responseStream = await result.Content.ReadAsStreamAsync();
			cancellationToken.ThrowIfCancellationRequested();

			var parsedRes = await JsonSerializer.DeserializeAsync<List<Opcodes>>(responseStream, cancellationToken: cancellationToken);

			if (parsedRes == null) { throw new HttpRequestException("GitHub returned null response"); }

			return parsedRes;
		}
	}
}
