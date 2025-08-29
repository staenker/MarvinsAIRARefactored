
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MarvinsAIRARefactored.Classes;

public sealed class ChromeSTTBridge : IDisposable
{
	// Serve files from: Documents\STT (must contain index.html + stt.js (+ optional style.css, etc.))
	public static string STTDirectory { get; private set; } = Path.Combine( App.DocumentsFolder, "STT" );

	private readonly HttpListener _http = new();
	private CancellationTokenSource _cts = new();
	private Task? _serverLoop;

	private readonly Lock _wsLock = new();
	private WebSocket? _clientWebSocket;

	private volatile bool _clientConnected;
	private TaskCompletionSource<bool> _connectedTcs = new( TaskCreationOptions.RunContinuationsAsynchronously );

	public int Port { get; }
	public string HostUrl => $"http://localhost:{Port}/";
	public bool IsClientConnected => _clientConnected;

	// Events from the page
	public event Action<string>? InterimText;
	public event Action<string>? FinalText;
	public event Action<string>? ErrorText;
	public event Action? SessionEnded;

	public ChromeSTTBridge( int port = 18888 )
	{
		Port = port;

		_http.Prefixes.Add( HostUrl );
	}

	// -------------------- Lifecycle --------------------

	public void Start()
	{
		if ( _cts.IsCancellationRequested )
		{
			Volatile.Write( ref _cts, new CancellationTokenSource() );
		}

		TryEnsureSTTDirectory();

		_connectedTcs = new( TaskCreationOptions.RunContinuationsAsynchronously );
		_clientConnected = false;

		_http.Start();
		_serverLoop = Task.Run( () => ServerLoop( _cts.Token ) );
	}

	public async Task StopAsync()
	{
		_cts.Cancel();

		try
		{
			_http.Stop();
		}
		catch
		{
		}

		try
		{
			_http.Close();
		}
		catch
		{
		}

		if ( _serverLoop != null )
		{
			try
			{
				await _serverLoop.ConfigureAwait( false );
			}
			catch
			{
			}
		}
	}

	public void Dispose() => _ = StopAsync();

	// -------------------- Host -> Page commands --------------------

	public Task SendStartAsync() => SendCommandAsync( new { type = "start" } );
	public Task SendStopAsync() => SendCommandAsync( new { type = "stop" } );
	public Task SendSetLangAsync( string lang ) => SendCommandAsync( new { type = "setlang", lang } );
	public Task<bool> WaitUntilConnectedAsync( TimeSpan timeout ) => _connectedTcs.Task.WaitAsync( timeout );
	public Task<bool> WaitUntilConnectedAsync() => _connectedTcs.Task;

	// -------------------- HTTP / WS server --------------------

	private async Task ServerLoop( CancellationToken ct )
	{
		while ( !ct.IsCancellationRequested )
		{
			HttpListenerContext ctx;

			try
			{
				ctx = await _http.GetContextAsync().ConfigureAwait( false );
			}
			catch when ( ct.IsCancellationRequested )
			{
				break;
			}
			catch
			{
				continue;
			}

			_ = Task.Run( () => HandleContextAsync( ctx, ct ), ct );
		}
	}

	private async Task HandleContextAsync( HttpListenerContext context, CancellationToken ct )
	{
		try
		{
			var path = context.Request.Url?.AbsolutePath.TrimStart( '/' ) ?? string.Empty;

			if ( context.Request.IsWebSocketRequest && path.StartsWith( "ws", StringComparison.OrdinalIgnoreCase ) )
			{
				var wsCtx = await context.AcceptWebSocketAsync( null ).ConfigureAwait( false );

				lock ( _wsLock ) _clientWebSocket = wsCtx.WebSocket;

				_clientConnected = true;
				_connectedTcs.TrySetResult( true );

				await ReceiveLoopAsync( wsCtx.WebSocket, ct ).ConfigureAwait( false );

				return;
			}

			await ServeStaticAsync( context, path, ct ).ConfigureAwait( false );
		}
		catch ( Exception ex )
		{
			TryWriteErrorResponse( context, 500, ex.Message );
		}
	}

	private async Task ReceiveLoopAsync( WebSocket ws, CancellationToken ct )
	{
		var buffer = new byte[ 64 * 1024 ];

		try
		{
			while ( ws.State == WebSocketState.Open && !ct.IsCancellationRequested )
			{
				var result = await ws.ReceiveAsync( buffer, ct ).ConfigureAwait( false );

				if ( result.MessageType == WebSocketMessageType.Close ) break;

				var json = Encoding.UTF8.GetString( buffer, 0, result.Count );

				ProcessInboundMessage( json );
			}
		}
		catch ( OperationCanceledException )
		{
		}
		catch ( Exception ex )
		{
			ErrorText?.Invoke( "WebSocket receive error: " + ex.Message );
		}
		finally
		{
			try
			{
				await ws.CloseAsync( WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None ).ConfigureAwait( false );
			}
			catch
			{
			}

			lock ( _wsLock ) _clientWebSocket = null;

			_clientConnected = false;

			if ( !_cts.IsCancellationRequested )
			{
				_connectedTcs = new( TaskCreationOptions.RunContinuationsAsynchronously );
			}
		}
	}

	private void ProcessInboundMessage( string json )
	{
		try
		{
			using var doc = JsonDocument.Parse( json );

			if ( !doc.RootElement.TryGetProperty( "type", out var tEl ) ) return;

			var type = tEl.GetString();

			switch ( type )
			{
				case "stt":
				{
					var interim = doc.RootElement.TryGetProperty( "interim", out var i ) ? i.GetString() ?? "" : "";
					var final = doc.RootElement.TryGetProperty( "final", out var f ) ? f.GetString() ?? "" : "";

					if ( !string.IsNullOrWhiteSpace( interim ) ) InterimText?.Invoke( interim );
					if ( !string.IsNullOrWhiteSpace( final ) ) FinalText?.Invoke( final );

					break;
				}

				case "end":
				{
					SessionEnded?.Invoke();
					break;
				}

				case "error":
				{
					var msg = doc.RootElement.TryGetProperty( "message", out var m ) ? m.GetString() ?? "unknown error" : "unknown error";

					ErrorText?.Invoke( msg );

					break;
				}
			}
		}
		catch
		{
			ErrorText?.Invoke( "Invalid JSON from page." );
		}
	}

	private async Task SendCommandAsync( object message )
	{
		WebSocket? ws;

		lock ( _wsLock ) ws = _clientWebSocket;

		if ( ws == null || ws.State != WebSocketState.Open ) return;

		var json = JsonSerializer.Serialize( message );
		var bytes = Encoding.UTF8.GetBytes( json );

		try
		{
			await ws.SendAsync( bytes, WebSocketMessageType.Text, true, CancellationToken.None ).ConfigureAwait( false );
		}
		catch ( Exception ex )
		{
			ErrorText?.Invoke( "WebSocket send error: " + ex.Message );
		}
	}

	// -------------------- Static file server --------------------

	private async Task ServeStaticAsync( HttpListenerContext context, string rawPath, CancellationToken ct )
	{
		var path = string.IsNullOrWhiteSpace( rawPath ) ? "index.html" : rawPath;

		if ( PathEndsWithSlash( path ) )
		{
			path = Path.Combine( path, "index.html" );
		}

		var fullBase = STTDirectory;
		var fullPath = SafeCombine( fullBase, path );

		if ( fullPath is null || !fullPath.StartsWith( Path.GetFullPath( fullBase ), StringComparison.OrdinalIgnoreCase ) )
		{
			TryWriteErrorResponse( context, 403, "Forbidden" );

			return;
		}

		if ( !File.Exists( fullPath ) )
		{
			var fallback = Path.Combine( fullBase, "index.html" );

			if ( !File.Exists( fallback ) )
			{
				TryWriteErrorResponse( context, 404, $"File not found: {path}" );
				return;
			}

			fullPath = fallback;
		}

		byte[] bytes;

		try
		{
			bytes = await File.ReadAllBytesAsync( fullPath, ct ).ConfigureAwait( false );
		}
		catch ( Exception ex )
		{
			TryWriteErrorResponse( context, 500, ex.Message );

			return;
		}

		var mime = GetMimeType( fullPath );

		context.Response.ContentType = mime;
		context.Response.ContentLength64 = bytes.LongLength;

		try
		{
			await context.Response.OutputStream.WriteAsync( bytes, ct ).ConfigureAwait( false );
		}
		finally
		{
			try
			{
				context.Response.OutputStream.Close();
			}
			catch
			{
			}
		}
	}

	private static bool PathEndsWithSlash( string path ) => path.EndsWith( "/", StringComparison.Ordinal ) || path.EndsWith( "\\", StringComparison.Ordinal );

	private static string? SafeCombine( string baseDir, string relativePath )
	{
		try
		{
			return Path.GetFullPath( Path.Combine( baseDir, relativePath.Replace( '/', Path.DirectorySeparatorChar ) ) );
		}
		catch
		{
			return null;
		}
	}

	private static string GetMimeType( string filePath )
	{
		var ext = Path.GetExtension( filePath ).ToLowerInvariant();

		return ext switch
		{
			".html" => "text/html; charset=utf-8",
			".htm" => "text/html; charset=utf-8",
			".js" => "application/javascript; charset=utf-8",
			".mjs" => "application/javascript; charset=utf-8",
			".css" => "text/css; charset=utf-8",
			".json" => "application/json; charset=utf-8",
			".png" => "image/png",
			".jpg" => "image/jpeg",
			".jpeg" => "image/jpeg",
			".gif" => "image/gif",
			".svg" => "image/svg+xml",
			".ico" => "image/x-icon",
			_ => "application/octet-stream"
		};
	}

	private static void TryWriteErrorResponse( HttpListenerContext ctx, int status, string message )
	{
		try
		{
			var body = Encoding.UTF8.GetBytes( $"<pre>{WebUtility.HtmlEncode( message )}</pre>" );

			ctx.Response.StatusCode = status;
			ctx.Response.StatusDescription = message;
			ctx.Response.ContentType = "text/html; charset=utf-8";
			ctx.Response.ContentLength64 = body.LongLength;
			ctx.Response.OutputStream.Write( body, 0, body.Length );
			ctx.Response.OutputStream.Close();
		}
		catch
		{
			/* ignore */
		}
	}

	private static void TryEnsureSTTDirectory()
	{
		try
		{
			if ( !Directory.Exists( STTDirectory ) )
			{
				Directory.CreateDirectory( STTDirectory );
			}
		}
		catch
		{
		}
	}
}
