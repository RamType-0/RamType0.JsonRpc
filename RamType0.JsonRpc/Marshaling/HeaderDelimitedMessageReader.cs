﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Marshaling
{
    public struct HeaderDelimitedMessageReader : IAsyncEnumerable<ArraySegment<byte>>
    {
        public HeaderDelimitedMessageReader(PipeReader input)
        {
            Input = input;
        }

        PipeReader Input { get; }
        public async IAsyncEnumerator<ArraySegment<byte>> GetAsyncEnumerator(CancellationToken cancellationToken)
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
