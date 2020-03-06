using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RamType0.JsonRpc.Marshaling
{
    public readonly struct Header
    {
        public Header(int contentLength)
        {
            this.contentLength = checked((uint)contentLength);
        }
        public Header(uint contentLength)
        {
            this.contentLength = contentLength;
        }


        public const int MaxHeaderSize = ContentLengthHeaderSize + MaxContentLengthChars + CRLFCRLFSize;
        const int MaxContentLengthChars = 10;
        const int ContentLengthHeaderSize = 16;//Content-Length: の文字数
        const int CRLFCRLFSize = 4;
        readonly uint contentLength;
        public int ContentLength => (int)contentLength;
        //public string ContentType { get; } //= "application/vscode-jsonrpc; charset=utf-8"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Write(Span<byte> buffer)
        {
            Contract.Requires(buffer.Length >= MaxHeaderSize);
            return WriteUnsafe(ref MemoryMarshal.GetReference(buffer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int WriteUnsafe(ref byte bufferRef)
        {
           // ref byte bufferRef = ref MemoryMarshal.GetReference(buffer);
            ReadOnlySpan<byte> contentLengthHeader = stackalloc byte[] { (byte)'C', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t', (byte)'-', (byte)'L', (byte)'e', (byte)'n', (byte)'g', (byte)'t', (byte)'h', (byte)':', (byte)' ', };
            ref var headerRef = ref MemoryMarshal.GetReference(contentLengthHeader);
            Unsafe.CopyBlockUnaligned(ref bufferRef, ref headerRef, ContentLengthHeaderSize);
            bufferRef = ref Unsafe.AddByteOffset(ref bufferRef, (IntPtr)ContentLengthHeaderSize);
            System.Buffers.Text.Utf8Formatter.TryFormat(contentLength, MemoryMarshal.CreateSpan(ref bufferRef,10), out var intChars);
            const uint crlfcrlf = ('\r') | ('\n' << 8) | ('\r' << 16) | ('\n' << 24);
            Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref bufferRef, (IntPtr)intChars),crlfcrlf);
            const int Offset = ContentLengthHeaderSize + CRLFCRLFSize;
            return (Offset + intChars);//Content-Length: (contentLength)CRLFCRLF
        }

    }
}
