using RamType0.JsonRpc.Internal;
using RamType0.JsonRpc.Marshaling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ValueTaskSupplement;
namespace RamType0.JsonRpc.Marshaling
{
    public sealed class PipeIOHeaderDelimitedRpcDomain : RpcDomain
    {
        PipeReader Input { get; }
        PipeWriter Output { get; }
        public PipeIOHeaderDelimitedRpcDomain(PipeReader input,PipeWriter output)
        {
            Input = input;
            Output = output;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {

            var resolve = ResolveMessagesAsync(cancellationToken);
            //var send = Task.Run(async () => await SendMessagesAsync(cancellationToken));
            var send = SendMessagesAsyncThroughPut(65536, cancellationToken);
            return ValueTaskEx.WhenAll(resolve, send);
        }

        async ValueTask SendMessagesAsyncThroughPut(int bufferedMessages = 32, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            IAsyncEnumerator<MessageHandle>? enumerator = null;
            try
            {
                enumerator = MessageChannel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator();
                var unFlushedMessages = new ArraySegment<MessageHandle>(ArrayPool<MessageHandle>.Shared.Rent(bufferedMessages), 0, bufferedMessages);
                
                var unFlushedMessagesCount = 0;
                try
                {


                    while (true)
                    {
                        var moveNext = enumerator.MoveNextAsync();
                        if (moveNext.IsCompleted)
                        {
                            if (moveNext.Result)
                            {
                                var message = enumerator.Current;
                                WriteMessage(Output, message.SerializedMessage);
                                if (QueueMessage(unFlushedMessages, message, ref unFlushedMessagesCount))
                                {
                                    await Output.FlushAsync(cancellationToken);
                                    SendComplete(unFlushedMessages);
                                    unFlushedMessagesCount = 0;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            await Output.FlushAsync(cancellationToken);
                            SendComplete(unFlushedMessages.AsSpan(..unFlushedMessagesCount));
                            unFlushedMessagesCount = 0;
                            if (await moveNext)
                            {
                                var message = enumerator.Current;
                                WriteMessage(Output, message.SerializedMessage);
                                if (QueueMessage(unFlushedMessages, message, ref unFlushedMessagesCount))
                                {
                                    await Output.FlushAsync(cancellationToken);
                                    SendComplete(unFlushedMessages);
                                    unFlushedMessagesCount = 0;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }


                    }
                }
                finally
                {
                    ArrayPool<MessageHandle>.Shared.Return(unFlushedMessages.Array!, true);
                }
            }
            finally
            {
                if (!(enumerator is null))
                {
                    await enumerator.DisposeAsync();
                }
            }
            static bool QueueMessage(Span<MessageHandle> queuedHandles,MessageHandle newHandle,ref int count)
            {
                ref var dst = ref Unsafe.Add(ref MemoryMarshal.GetReference(queuedHandles), count++);
                dst = newHandle;
                return queuedHandles.Length == count;
            }

            static void SendComplete(ReadOnlySpan<MessageHandle> completedHandles)
            {
                for (int i = 0; i < completedHandles.Length; i++)
                {
                    completedHandles[i].SendComplete();
                }
            }

        }
        async ValueTask SendMessagesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await foreach(var message in MessageChannel.Reader.ReadAllAsync(cancellationToken))
            {
                WriteMessage(Output, message.SerializedMessage);
                var flushResult = await Output.FlushAsync(cancellationToken);
                if (flushResult.IsCanceled)
                {
                    break;
                }
                message.SendComplete();
            }
        }
        static void WriteMessage(PipeWriter writer, ReadOnlySpan<byte> serializedResponse)
        {
            var span = writer.GetSpan(Header.MaxHeaderSize + serializedResponse.Length);
            var header = new Header(serializedResponse.Length);
            var headerSize = header.Write(span);
            serializedResponse.CopyTo(span[headerSize..]);
            writer.Advance(headerSize + serializedResponse.Length);
        }
        async ValueTask ResolveMessagesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await foreach (var message in ReadMessageSegmentsAsync(cancellationToken))
            {
                _ = ResolveMessageAsync(message);
            }
        }


        async IAsyncEnumerable<ArraySegment<byte>> ReadMessageSegmentsAsync([EnumeratorCancellation]CancellationToken cancellationToken = default)
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
                        if (Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(headerEnd)) == (('\r') | ('\n' << 8) | ('\r' << 16) | ('\n' << 24)))
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
