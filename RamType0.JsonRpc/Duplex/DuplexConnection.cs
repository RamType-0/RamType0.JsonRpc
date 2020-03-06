using RamType0.JsonRpc.Internal;
using RamType0.JsonRpc.Protocol;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Duplex
{
    public sealed class DuplexConnection
    {
        RpcDomain Domain { get; }
        PipeReader Input { get; }
        ConcurrentDictionary<EscapedUTF8String, RpcAsyncMethodEntry> MethodEntries { get; } = new ConcurrentDictionary<EscapedUTF8String, RpcAsyncMethodEntry>();
        public bool AddMethod(string name,RpcAsyncMethodEntry methodEntry)
        {
            return MethodEntries.TryAdd(EscapedUTF8String.FromUnEscaped(name), methodEntry);
        }
        public bool RemoveMethod(string name,[NotNullWhen(true)]out RpcAsyncMethodEntry? methodEntry)
        {
            return MethodEntries.TryRemove(EscapedUTF8String.FromUnEscaped(name), out methodEntry);
        }
        IJsonFormatterResolver JsonFormatterResolver { get; }
        public async ValueTask ResolveMessagesAsync(CancellationToken cancellationToken = default)
        {
            await foreach (var message in ReadMessageSegmentsAsync(cancellationToken))
            {
                var parseResult = MessageParser.ParseDuplexMessage(message);
                switch (parseResult.MessageKind)
                {
                    case MessageKind.InvalidJson:
                        {
                            var response = ErrorResponse.ParseError(message);
                            _ = Domain.ResponseAsync(JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled());
                            goto ReleaseMessage;
                        }
                    case MessageKind.InvalidMessage:
                        {
                            var response = ErrorResponse.InvalidRequest(message);
                            _ = Domain.ResponseAsync(JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled());
                            goto ReleaseMessage;
                        }
                    case MessageKind.ClientMessage:
                        {
                            if(MethodEntries.TryGetValue(parseResult.Method,out var entry))
                            {
                                var parameters = parseResult.Params;
                                var id = parseResult.id;
                                _ = Task.Run(async() => 
                                { 
                                    var response = await entry.ResolveRequestAsync(parameters, id); 
                                    ArrayPool<byte>.Shared.Return(message.Array!);
                                    if (!(response.Array is null))
                                    {
                                        _ = Domain.ResponseAsync(response);
                                    }
                                    

                                });
                                break;
                            }
                            else
                            {
                                if(parseResult.id is ID reqID)
                                {
                                    var response = JsonSerializer.SerializeUnsafe(ErrorResponse.MethodNotFound(reqID, parseResult.Method.ToString())).CopyToPooled();
                                    _ = Domain.ResponseAsync(response);
                                }
                                goto ReleaseMessage;
                            }
                        }
                    case MessageKind.ResultResponse:
                        {
                            if (parseResult.id is ID id)
                            {
                                Domain.SetResult(id, parseResult.Result);
                            }
                            else
                            {
                                Debug.Fail("Result without id");
                            }
                            goto ReleaseMessage;

                        }
                    case MessageKind.ErrorResponse:
                        {
                            Domain.SetError(parseResult.id, parseResult.Error);
                            goto ReleaseMessage;
                        }
                    ReleaseMessage:
                        {
                            ArrayPool<byte>.Shared.Return(message.Array!);
                            break;
                        }
                }
            }
        }

        async IAsyncEnumerable<ArraySegment<byte>> ReadMessageSegmentsAsync([EnumeratorCancellation]CancellationToken cancellationToken)
        {
            while (true)
            {
                //ReadOnlySpan<byte> contentLength = stackalloc byte[] { (byte)'C', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t', (byte)'-', (byte)'L', (byte)'e', (byte)'n', (byte)'g', (byte)'t', (byte)'h', (byte)':', (byte)' ', };
                //ReadOnlySpan<byte> CRLFCRLF = stackalloc byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n', };

                var readResult = await Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (readResult.IsCanceled)
                {
                    yield break;
                }
                var buffer = readResult.Buffer;
                if (!TryFindHeaderEnd(buffer, out var pos))
                {
                    while (true)
                    {
                        Input.AdvanceTo(buffer.Start);
                        readResult = await Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                        if (readResult.IsCanceled)
                        {
                            yield break;
                        }
                        buffer = readResult.Buffer;
                        if (TryFindHeaderEnd(buffer.Slice(pos), out pos))
                        {
                            break;
                        }
                    }
                }

                ////ヘッダーが読めた
                var contentLength = GetContentLength(buffer);
                buffer = buffer.Slice(pos);
                while (buffer.Length < contentLength)
                {
                    Input.AdvanceTo(pos);
                    readResult = await Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    if (readResult.IsCanceled)
                    {
                        yield break;
                    }
                    buffer = readResult.Buffer;
                }
                var jsonBuffer = buffer.Slice(pos, contentLength);
                var json = new ArraySegment<byte>(ArrayPool<byte>.Shared.Rent(contentLength), 0, contentLength);//ArrayPoolの内部実装的にこのタスクのあるスレッドローカルのArrayPoolが枯渇しやすいが仕方なさそう
                jsonBuffer.CopyTo(json);
                Input.AdvanceTo(jsonBuffer.End);
                yield return json;


            }
            static bool TryFindHeaderEnd(ReadOnlySequence<byte> buffer, out SequencePosition headerTerminalOrNextSearchContinuation)
            {
                var pos = buffer.PositionOf((byte)'\r');
                if (pos is SequencePosition crPos)
                {
                    var crSlice = buffer.Slice(crPos);
                    if (crSlice.Length < 4)
                    {
                        headerTerminalOrNextSearchContinuation = crPos;
                        return false;

                    }
                    else
                    {
                        Span<byte> headerEnd = stackalloc byte[4];
                        ReadOnlySequence<byte> headerEndSeq = crSlice.Slice(0, 4);
                        headerEndSeq.CopyTo(headerEnd);
                        if (headerEnd.SequenceEqual(stackalloc byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n', }))
                        {
                            headerTerminalOrNextSearchContinuation = headerEndSeq.End;
                            return true;
                        }
                        else
                        {
                            return TryFindHeaderEnd(buffer.Slice(headerEndSeq.End), out headerTerminalOrNextSearchContinuation);
                        }
                    }
                }
                else
                {
                    headerTerminalOrNextSearchContinuation = buffer.End;
                    return false;
                }
            }
            static int GetContentLength(ReadOnlySequence<byte> header)
            {
                const int CONTENT_LENGTH_VALUE_START = 16;
                var src = header.Slice(CONTENT_LENGTH_VALUE_START);
                if (src.Length > 10)
                {
                    src = src.Slice(0, 10);
                }
                Span<byte> buffer = stackalloc byte[(int)src.Length];
                src.CopyTo(buffer);
                int contentLength = 0;
                foreach (var c in buffer)
                {
                    var number = c - (byte)'0';
                    if ((uint)number < 10)
                    {

                        contentLength *= 10;
                        contentLength += number;//.NETにmadなんてありません=>using System.Runtime.Intrinsics.X86.Fma;があったけどこの場合使えない
                    }
                    else
                    {
                        return contentLength;
                    }

                }
                return -1;
            }

        }

    }
}
